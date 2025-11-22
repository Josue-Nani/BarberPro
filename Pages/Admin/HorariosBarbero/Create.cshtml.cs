using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;

namespace BarberPro.Pages.Admin.HorariosBarbero
{
    public class CreateModel : PageModel
    {
        private readonly BarberContext _context;

        public CreateModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HorarioBarbero Horario { get; set; } = new();

        public List<Barbero> Barberos { get; set; } = new();

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

            // Check for overlapping schedules
            var overlapping = await _context.HorariosBarbero
                .AnyAsync(h => h.BarberoID == Horario.BarberoID &&
                              h.Fecha.Date == Horario.Fecha.Date &&
                              ((h.HoraInicio < Horario.HoraFin && h.HoraFin > Horario.HoraInicio)));

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
