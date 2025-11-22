using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BarberPro.Pages.Admin.Usuarios
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly BarberContext _context;
        public EditModel(BarberContext context) => _context = context;

        [BindProperty]
        public Usuario Usuario { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        public List<Rol> Roles { get; set; }

        public async Task<IActionResult> OnGet(int id)
        {
            Usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioID == id);
            if (Usuario == null) return RedirectToPage("/Admin/Usuarios/Index");
            Roles = await _context.Roles.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                // Reload Roles for the dropdown when returning to the page with validation errors
                Roles = await _context.Roles.ToListAsync();
                return Page();
            }

            var existing = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioID == Usuario.UsuarioID);
            if (existing == null) return RedirectToPage("/Admin/Usuarios/Index");

            existing.NombreCompleto = Usuario.NombreCompleto;
            existing.Correo = Usuario.Correo;
            existing.Telefono = Usuario.Telefono;
            existing.FotoPerfil = Usuario.FotoPerfil;
            existing.RolID = Usuario.RolID;

            if (!string.IsNullOrEmpty(NewPassword))
            {
                existing.ContrasenaHash = ComputeSha256Hash(NewPassword);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Admin/Usuarios/Index");
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}