using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.HorariosBarbero
{
    public class CreateModel : PageModel
    {
        private readonly BarberContext _context;
        private readonly Services.DisponibilidadService _disponibilidadService;

        public CreateModel(BarberContext context, Services.DisponibilidadService disponibilidadService)
        {
            _context = context;
            _disponibilidadService = disponibilidadService;
        }

        [BindProperty]
        public HorarioBarbero Horario { get; set; } = new();

        public List<BarberoModel> Barberos { get; set; } = new();

        public async Task OnGetAsync()
        {
            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => b.Usuario != null)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            // Validate time range
            if (Horario.HoraFin <= Horario.HoraInicio)
            {
                ModelState.AddModelError("Horario.HoraFin", "La hora de fin debe ser posterior a la hora de inicio");
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            // Validate FechaFin if provided
            if (Horario.FechaFin.HasValue && Horario.Fecha.HasValue && Horario.FechaFin.Value.Date < Horario.Fecha.Value.Date)
            {
                ModelState.AddModelError("Horario.FechaFin", "La fecha fin debe ser igual o posterior a la fecha inicio");
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            // Check if the date is configured as a day off (only for single-day schedules)
            if (!Horario.FechaFin.HasValue && Horario.Fecha.HasValue)
            {
                var esDiaLibre = await _disponibilidadService.EsDiaLibre(Horario.BarberoID, Horario.Fecha.Value);
                if (esDiaLibre)
                {
                    ModelState.AddModelError("Horario.Fecha", "No se puede crear un horario en una fecha configurada como día libre para este barbero");
                    Barberos = await _context.Barberos
                        .Include(b => b.Usuario)
                        .ToListAsync();
                    return Page();
                }
            }

            // Check for overlapping schedules (updated to handle period ranges)
            var overlapping = await _context.HorariosBarbero
                .AnyAsync(h => h.BarberoID == Horario.BarberoID &&
                              ((h.Fecha <= (Horario.FechaFin ?? Horario.Fecha) && 
                                (h.FechaFin ?? h.Fecha) >= Horario.Fecha)));

            if (overlapping)
            {
                ModelState.AddModelError("", "Ya existe un horario que se solapa con este en la misma fecha");
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            _context.HorariosBarbero.Add(Horario);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
