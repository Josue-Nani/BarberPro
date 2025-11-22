using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;

namespace BarberPro.Pages.Admin.HorariosBarbero
{
    public class DeleteModel : PageModel
    {
        private readonly BarberContext _context;

        public DeleteModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HorarioBarbero Horario { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Horario = await _context.HorariosBarbero
                .Include(h => h.Barbero)
                    .ThenInclude(b => b!.Usuario)
                .FirstOrDefaultAsync(m => m.HorarioID == id) ?? new HorarioBarbero();

            if (Horario.HorarioID == 0)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var horario = await _context.HorariosBarbero.FindAsync(id);

            if (horario != null)
            {
                _context.HorariosBarbero.Remove(horario);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
