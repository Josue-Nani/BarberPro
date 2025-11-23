using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.HorariosBarbero
{
    public class EditModel : PageModel
    {
        private readonly BarberContext _context;

        public EditModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HorarioBarbero Horario { get; set; } = new();

        public List<BarberoModel> Barberos { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Horario = await _context.HorariosBarbero.FindAsync(id);

            if (Horario == null)
            {
                return NotFound();
            }

            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .ToListAsync();

            return Page();
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

            _context.Attach(Horario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HorarioExists(Horario.HorarioID))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task<bool> HorarioExists(int id)
        {
            return await _context.HorariosBarbero.AnyAsync(e => e.HorarioID == id);
        }
    }
}
