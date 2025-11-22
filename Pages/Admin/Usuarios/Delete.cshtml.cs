using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Pages.Admin.Usuarios
{
    [Authorize(Policy = "AdminOnly")]
    public class DeleteModel : PageModel
    {
        private readonly BarberContext _context;
        
        public DeleteModel(BarberContext context) => _context = context;

        [BindProperty]
        public Usuario Usuario { get; set; } = new Usuario();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioID == id) ?? new Usuario();
            
            if (Usuario.UsuarioID == 0) return RedirectToPage("./Index");
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _context.Usuarios.FindAsync(Usuario.UsuarioID);
            
            if (existing != null)
            {
                _context.Usuarios.Remove(existing);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToPage("./Index");
        }
    }
}
