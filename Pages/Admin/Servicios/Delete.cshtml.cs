using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Pages.Admin.Servicios
{
    [Authorize(Policy = "AdminOnly")]
    public class DeleteModel : PageModel
    {
        private readonly BarberContext _context;
        
        public DeleteModel(BarberContext context) => _context = context;

        [BindProperty]
        public Servicio Servicio { get; set; } = new Servicio();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.ServicioID == id) ?? new Servicio();
            
            if (Servicio.ServicioID == 0) return RedirectToPage("./Index");
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var s = await _context.Servicios.FindAsync(Servicio.ServicioID);
            
            if (s != null)
            {
                _context.Servicios.Remove(s);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToPage("./Index");
        }
    }
}
