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

        // Dependent record counts
        public int BarberosCount { get; set; }
        public int ClientesCount { get; set; }
        public int ReservasCount { get; set; }
        public int HorariosBarberoCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioID == id) ?? new Usuario();
            
            if (Usuario.UsuarioID == 0) return RedirectToPage("./Index");
            
            // Count dependent records
            BarberosCount = await _context.Barberos
                .Where(b => b.UsuarioID == id)
                .CountAsync();
            
            ClientesCount = await _context.Clientes
                .Where(c => c.UsuarioID == id)
                .CountAsync();
            
            // Count reservas associated with this user's barbero or cliente records
            var barberoIds = await _context.Barberos
                .Where(b => b.UsuarioID == id)
                .Select(b => b.BarberoID)
                .ToListAsync();
            
            var clienteIds = await _context.Clientes
                .Where(c => c.UsuarioID == id)
                .Select(c => c.ClienteID)
                .ToListAsync();
            
            ReservasCount = await _context.Reservas
                .Where(r => barberoIds.Contains(r.BarberoID) || clienteIds.Contains(r.ClienteID))
                .CountAsync();
            
            // Count horarios associated with this user's barbero records
            HorariosBarberoCount = await _context.HorariosBarbero
                .Where(h => barberoIds.Contains(h.BarberoID))
                .CountAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existing = await _context.Usuarios.FindAsync(Usuario.UsuarioID);
            
            if (existing == null)
            {
                return RedirectToPage("./Index");
            }

            // Step 1: Delete all Reservas associated with this user's Barbero or Cliente records
            var barberoIds = await _context.Barberos
                .Where(b => b.UsuarioID == Usuario.UsuarioID)
                .Select(b => b.BarberoID)
                .ToListAsync();
            
            var clienteIds = await _context.Clientes
                .Where(c => c.UsuarioID == Usuario.UsuarioID)
                .Select(c => c.ClienteID)
                .ToListAsync();
            
            var reservasToDelete = await _context.Reservas
                .Where(r => barberoIds.Contains(r.BarberoID) || clienteIds.Contains(r.ClienteID))
                .ToListAsync();
            
            if (reservasToDelete.Any())
            {
                _context.Reservas.RemoveRange(reservasToDelete);
            }

            // Step 2: Delete all HorariosBarbero records for this user's barberos
            var horariosToDelete = await _context.HorariosBarbero
                .Where(h => barberoIds.Contains(h.BarberoID))
                .ToListAsync();
            
            if (horariosToDelete.Any())
            {
                _context.HorariosBarbero.RemoveRange(horariosToDelete);
            }

            // Step 3: Delete all Barbero records for this user
            var barberosToDelete = await _context.Barberos
                .Where(b => b.UsuarioID == Usuario.UsuarioID)
                .ToListAsync();
            
            if (barberosToDelete.Any())
            {
                _context.Barberos.RemoveRange(barberosToDelete);
            }

            // Step 4: Delete all Cliente records for this user
            var clientesToDelete = await _context.Clientes
                .Where(c => c.UsuarioID == Usuario.UsuarioID)
                .ToListAsync();
            
            if (clientesToDelete.Any())
            {
                _context.Clientes.RemoveRange(clientesToDelete);
            }

            // Step 5: Delete the Usuario record
            _context.Usuarios.Remove(existing);
            
            // Save all changes
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Usuario eliminado exitosamente junto con todos sus registros asociados.";
            return RedirectToPage("./Index");
        }
    }
}
