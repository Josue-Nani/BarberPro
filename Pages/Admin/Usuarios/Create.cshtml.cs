using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;

namespace BarberPro.Pages.Admin.Usuarios
{
    public class CreateModel : PageModel
    {
        private readonly BarberContext _context;

        public CreateModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string NombreCompleto { get; set; } = string.Empty;

        [BindProperty]
        public string Correo { get; set; } = string.Empty;

        [BindProperty]
        public string? Telefono { get; set; }

        [BindProperty]
        public IFormFile? FotoFile { get; set; }

        [BindProperty]
        public int RolID { get; set; }

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        public List<Rol> Roles { get; set; } = new List<Rol>();

        public async Task OnGetAsync()
        {
            Roles = await _context.Roles.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Roles = await _context.Roles.ToListAsync();
                return Page();
            }

            var passwordHash = HashPassword(NewPassword);

            string? fotoPath = null;
            if (FotoFile != null && FotoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(FotoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await FotoFile.CopyToAsync(fileStream);
                }
                
                fotoPath = "/uploads/profiles/" + uniqueFileName;
            }

            var usuario = new Usuario
            {
                NombreCompleto = NombreCompleto,
                Correo = Correo,
                ContrasenaHash = passwordHash,
                Telefono = Telefono,
                FotoPerfil = fotoPath,
                RolID = RolID,
                FechaRegistro = DateTime.Now,
                Estado = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Si el rol asignado es 'Cliente', también se crea un registro en la tabla Clientes
            var assignedRole = await _context.Roles.FindAsync(RolID);
            if (assignedRole != null && string.Equals(assignedRole.NombreRol, "Cliente", StringComparison.OrdinalIgnoreCase))
            {
                var cliente = new Cliente
                {
                    UsuarioID = usuario.UsuarioID,
                    Direccion = null,
                    FechaNacimiento = null
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            var sb = new System.Text.StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
