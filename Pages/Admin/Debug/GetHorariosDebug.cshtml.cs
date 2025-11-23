using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BarberPro.Pages.Admin.Debug
{
    [Authorize(Policy = "AdminOnly")]
    public class GetHorariosDebugModel : PageModel
    {
        private readonly BarberContext _context;
        private readonly ILogger<GetHorariosDebugModel> _logger;

        public GetHorariosDebugModel(BarberContext context, ILogger<GetHorariosDebugModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int? barberoId, DateTime? fecha)
        {
            if (!barberoId.HasValue || !fecha.HasValue)
            {
                return BadRequest(new { error = "barberoId and fecha are required. Example: /Admin/Debug/GetHorariosDebug?barberoId=1&fecha=2025-11-23" });
            }

            var targetDate = fecha.Value.Date;
            var nextDate = targetDate.AddDays(1);

            try
            {
                // DisponibilidadService removed

                var horarios = await _context.HorariosBarbero
                    .Where(h => h.BarberoID == barberoId.Value && h.Disponible && h.Fecha.HasValue && (
                        (h.Fecha.Value.Date == targetDate) || (h.FechaFin.HasValue && h.Fecha.Value.Date <= targetDate && h.FechaFin.Value.Date >= targetDate)
                    ))
                    .Select(h => new
                    {
                        h.HorarioID,
                        h.BarberoID,
                        Fecha = h.Fecha,
                        FechaFin = h.FechaFin,
                        HoraInicio = h.HoraInicio,
                        HoraFin = h.HoraFin,
                        h.Disponible,
                        DiasLibres = new {
                            h.LunesLibre,
                            h.MartesLibre,
                            h.MiercolesLibre,
                            h.JuevesLibre,
                            h.ViernesLibre,
                            h.SabadoLibre,
                            h.DomingoLibre
                        }
                    })
                    .OrderBy(h => h.HoraInicio)
                    .ToListAsync();

                var reservas = await _context.Reservas
                    .Where(r => r.BarberoID == barberoId.Value && r.FechaReserva >= targetDate && r.FechaReserva < nextDate)
                    .Select(r => new {
                        r.ReservaID,
                        r.ClienteID,
                        r.BarberoID,
                        r.ServicioID,
                        r.FechaReserva,
                        r.HoraInicio,
                        r.HoraFin,
                        r.Estado
                    })
                    .ToListAsync();

                // ConfiguracionesDisponibilidad table removed

                var result = new
                {
                    barberoId = barberoId.Value,
                    fecha = targetDate.ToString("yyyy-MM-dd"),
                    horarios,
                    reservas
                };

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHorariosDebug");
                return StatusCode(500, new { error = "Error interno", detail = ex.Message });
            }
        }
    }
}
