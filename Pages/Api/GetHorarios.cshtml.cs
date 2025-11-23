using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BarberPro.Pages.Api
{
    public class GetHorariosModel : PageModel
    {
        private readonly BarberContext _context;
        private readonly ILogger<GetHorariosModel> _logger;

        public GetHorariosModel(BarberContext context, ILogger<GetHorariosModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Accept optional servicioId so API can determine duration server-side if client didn't send it
        public async Task<IActionResult> OnGetAsync(int? barberoId, DateTime? fecha, int duracion = 0, int? servicioId = null)
        {
            try
            {
                _logger.LogInformation("GetHorarios called with barberoId={BarberoId}, fecha={Fecha}, duracion={Duracion}, servicioId={ServicioId}", barberoId, fecha, duracion, servicioId);

                if (!barberoId.HasValue)
                {
                    _logger.LogWarning("GetHorarios: missing barberoId");
                    return BadRequest(new { error = "BarberoID es requerido" });
                }

                var targetDate = fecha?.Date ?? DateTime.Today;
                var nextDate = targetDate.AddDays(1);

                // Check if barbero is generally available
                var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.BarberoID == barberoId.Value);
                if (barbero == null)
                {
                    _logger.LogWarning("Barbero {BarberoId} not found", barberoId.Value);
                    return new JsonResult(new List<object>());
                }

                // Check barbero availability status
                var disponibilidad = barbero.Disponibilidad ?? "Disponible";
                if (disponibilidad != "Disponible")
                {
                    _logger.LogInformation("Barbero {BarberoId} is not available (status: {Status})", barberoId.Value, disponibilidad);
                    return new JsonResult(new List<object>());
                }

                // Determine service duration: prefer servicioId, then duracion parameter, then default 60
                int durationMinutes = 60;
                if (servicioId.HasValue)
                {
                    var svc = await _context.Servicios.FirstOrDefaultAsync(s => s.ServicioID == servicioId.Value);
                    if (svc != null && svc.DuracionMinutos > 0)
                    {
                        durationMinutes = svc.DuracionMinutos;
                    }
                }
                else if (duracion > 0)
                {
                    durationMinutes = duracion;
                }

                // Load horarios for that barbero and date. Include single-day horarios and period horarios that contain the target date.
                var horarios = await _context.HorariosBarbero
                    .Where(h => h.BarberoID == barberoId.Value && h.Disponible && h.Fecha.HasValue && (
                        (h.Fecha.Value >= targetDate && h.Fecha.Value < nextDate) || 
                        (h.FechaFin.HasValue && h.Fecha.Value <= targetDate && h.FechaFin.Value >= targetDate)
                    ))
                    .OrderBy(h => h.HoraInicio)
                    .ToListAsync();

                _logger.LogInformation("Found {CountHorarios} horarios for barbero {BarberoId} on {Fecha}", horarios.Count, barberoId.Value, targetDate.ToString("yyyy-MM-dd"));

                // Exclude horarios that explicitly mark this weekday as free (e.g., period with SabadoLibre true)
                var dow = targetDate.DayOfWeek;
                horarios = horarios.Where(h => !h.EsDiaLibre(dow)).ToList();

                _logger.LogInformation("{CountAfterFilter} horarios after weekday-free filter", horarios.Count);

                if (!horarios.Any())
                {
                    _logger.LogInformation("No horarios available for barbero {BarberoId} on {Fecha}", barberoId.Value, targetDate.ToString("yyyy-MM-dd"));
                    return new JsonResult(new List<object>());
                }

                // Load existing reservas for that barbero and date (exclude cancelled)
                var reservas = await _context.Reservas
                    .Where(r => r.BarberoID == barberoId.Value && 
                               r.FechaReserva >= targetDate && 
                               r.FechaReserva < nextDate &&
                               r.Estado != "Cancelada")
                    .ToListAsync();

                _logger.LogInformation("Found {CountReservas} active reservas for barbero {BarberoId} on {Fecha}", reservas.Count, barberoId.Value, targetDate.ToString("yyyy-MM-dd"));

                // Use a small fixed step (15 minutes) for slot generation
                int stepMinutes = 15;
                if (durationMinutes < stepMinutes && durationMinutes > 0)
                {
                    stepMinutes = Math.Max(5, durationMinutes);
                }

                var step = TimeSpan.FromMinutes(stepMinutes);
                var slots = new List<object>();
                var serviceDurationTs = TimeSpan.FromMinutes(durationMinutes);

                foreach (var h in horarios)
                {
                    var t = h.HoraInicio;
                    while (t + serviceDurationTs <= h.HoraFin)
                    {
                        var slotStart = t;
                        var slotEnd = t + serviceDurationTs;

                        // Check overlap with any existing reservation
                        var conflict = reservas.Any(r => r.HoraInicio < slotEnd && r.HoraFin > slotStart);

                        if (!conflict)
                        {
                            // Use targetDate as the slot's date even if horario.Fecha is earlier (period schedule)
                            slots.Add(new
                            {
                                horarioID = h.HorarioID,
                                fecha = targetDate.ToString("yyyy-MM-dd"),
                                fechaDisplay = targetDate.ToString("dd/MM/yyyy"),
                                horaInicio = slotStart.ToString(@"hh\:mm"),
                                horaFin = slotEnd.ToString(@"hh\:mm"),
                                disponible = true,
                                displayText = $"{targetDate:dd/MM/yyyy} - {slotStart.ToString(@"hh\:mm")} a {slotEnd.ToString(@"hh\:mm")}"
                            });
                        }

                        t = t.Add(step);
                    }
                }

                _logger.LogInformation("Returning {CountSlots} available slots for barbero {BarberoId} on {Fecha}", slots.Count, barberoId.Value, targetDate.ToString("yyyy-MM-dd"));
                return new JsonResult(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHorarios: {Message}", ex.Message);
                return new JsonResult(new { error = $"Error interno: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
