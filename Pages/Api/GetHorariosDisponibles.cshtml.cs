using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using System.Text.Json;

namespace BarberPro.Pages
{
    public class GetHorariosDisponiblesModel : PageModel
    {
        private readonly BarberContext _context;

        public GetHorariosDisponiblesModel(BarberContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int barberoId, string fecha, int servicioId)
        {
            if (!DateTime.TryParse(fecha, out var parsedFecha))
            {
                return new JsonResult(new { error = "Fecha inválida" });
            }

            var servicio = await _context.Servicios.FindAsync(servicioId);
            if (servicio == null)
            {
                return new JsonResult(new { error = "Servicio no encontrado" });
            }

            var duracionServicio = TimeSpan.FromMinutes(servicio.DuracionMinutos);

            // Get barbero's schedule blocks for the date
            var horarios = await _context.HorariosBarbero
                .Where(h => h.BarberoID == barberoId && h.Fecha.Date == parsedFecha.Date && h.Disponible)
                .ToListAsync();

            // Get existing reservations for that barbero on that date (excluding cancelled ones)
            var reservas = await _context.Reservas
                .Where(r => r.BarberoID == barberoId && r.FechaReserva.Date == parsedFecha.Date && r.Estado != "Cancelada")
                .ToListAsync();

            var slotsDisponibles = new List<object>();

            // Calculate step (15 min intervals by default)
            var step = TimeSpan.FromMinutes(15);

            foreach (var horario in horarios)
            {
                for (var t = horario.HoraInicio; t + duracionServicio <= horario.HoraFin; t = t.Add(step))
                {
                    var slotStart = t;
                    var slotEnd = t + duracionServicio;

                    // Check if this slot conflicts with any existing reservation
                    var conflict = reservas.Any(r => r.HoraInicio < slotEnd && r.HoraFin > slotStart);

                    slotsDisponibles.Add(new
                    {
                        horaInicio = slotStart.ToString(@"hh\:mm"),
                        horaFin = slotEnd.ToString(@"hh\:mm"),
                        disponible = !conflict,
                        display = $"{slotStart:hh\\:mm} - {slotEnd:hh\\:mm}"
                    });
                }
            }

            return new JsonResult(new
            {
                fecha = parsedFecha.ToString("yyyy-MM-dd"),
                barberoId = barberoId,
                servicioId = servicioId,
                duracionMinutos = servicio.DuracionMinutos,
                slots = slotsDisponibles
            });
        }
    }
}
