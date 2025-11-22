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

        public GetHorariosModel(BarberContext context)
        {
            _context = context;
        }

        // Accept optional servicioId so API can determine duration server-side if client didn't send it
        public IActionResult OnGet(int? barberoId, DateTime? fecha, int duracion = 0, int? servicioId = null)
        {
            if (!barberoId.HasValue)
            {
                return new JsonResult(new { error = "BarberoID es requerido" });
            }

            var targetDate = fecha?.Date ?? DateTime.Today;

            // Determine service duration: prefer servicioId, then duracion parameter, then default 60
            int durationMinutes = 60;
            if (servicioId.HasValue)
            {
                var svc = _context.Servicios.FirstOrDefault(s => s.ServicioID == servicioId.Value);
                if (svc != null && svc.DuracionMinutos > 0)
                {
                    durationMinutes = svc.DuracionMinutos;
                }
            }
            else if (duracion > 0)
            {
                durationMinutes = duracion;
            }

            // Load horarios for that barbero and date
            var horarios = _context.HorariosBarbero
                .Where(h => h.BarberoID == barberoId && h.Disponible && h.Fecha.Date == targetDate)
                .OrderBy(h => h.HoraInicio)
                .ToList();

            // Load existing reservas for that barbero and date
            var reservas = _context.Reservas
                .Where(r => r.BarberoID == barberoId && r.FechaReserva.Date == targetDate)
                .ToList();

            // Use a small fixed step (5 minutes) so we don't skip valid start times like :20 for 20-minute services
            int stepMinutes = 5;
            if (durationMinutes < stepMinutes && durationMinutes > 0)
            {
                stepMinutes = Math.Max(1, durationMinutes);
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
                        slots.Add(new
                        {
                            horarioID = h.HorarioID,
                            fecha = h.Fecha.ToString("yyyy-MM-dd"),
                            fechaDisplay = h.Fecha.ToString("dd/MM/yyyy"),
                            horaInicio = slotStart.ToString(@"hh\:mm"),
                            horaFin = slotEnd.ToString(@"hh\:mm"),
                            disponible = true,
                            displayText = $"{h.Fecha:dd/MM/yyyy} - {slotStart:hh\\:mm} a {slotEnd:hh\\:mm}"
                        });
                    }

                    t = t.Add(step);
                }
            }

            return new JsonResult(slots);
        }
    }
}
