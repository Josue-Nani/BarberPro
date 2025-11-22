using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft. EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BarberPro.Pages
{
    [Authorize]
    public class MiPerfilModel : PageModel
    {
        private readonly BarberContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MiPerfilModel> _logger;

        public MiPerfilModel(BarberContext context, IWebHostEnvironment environment, ILogger<MiPerfilModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public Usuario? Usuario { get; set; }

        [BindProperty]
        public EditProfileDTO? EditProfile { get; set; }

        [BindProperty]
        public ChangePasswordDTO? ChangePassword { get; set; }

        [BindProperty]
        public IFormFile? FotoFile { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Index");
            }

            Usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo == userEmail);

            if (Usuario == null)
            {
                return NotFound();
            }

            EditProfile = new EditProfileDTO
            {
                NombreCompleto = Usuario.NombreCompleto,
                Correo = Usuario.Correo,
                Telefono = Usuario.Telefono,
                FotoPerfilActual = Usuario.FotoPerfil
            };

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Index");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == userEmail);
            if (usuario == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid || EditProfile == null)
            {
                ErrorMessage = "Por favor corrige los errores en el formulario";
                Usuario = usuario;
                return Page();
            }

            try
            {
                if (EditProfile.Correo != usuario.Correo)
                {
                    var emailExists = await _context.Usuarios
                        .AnyAsync(u => u.Correo == EditProfile.Correo && u.UsuarioID != usuario.UsuarioID);

                    if (emailExists)
                    {
                        ErrorMessage = "El correo electrónico ya está en uso";
                        Usuario = usuario;
                        return Page();
                    }
                }

                usuario.NombreCompleto = EditProfile.NombreCompleto;
                usuario.Correo = EditProfile.Correo;
                usuario.Telefono = EditProfile.Telefono;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Perfil actualizado correctamente";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                ErrorMessage = "Error al actualizar el perfil";
                Usuario = usuario;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUploadPhotoAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Index");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == userEmail);
            if (usuario == null)
            {
                return NotFound();
            }

            if (FotoFile == null || FotoFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor selecciona una imagen";
                return RedirectToPage();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(FotoFile.FileName).ToLowerInvariant();
            
            if (!Array.Exists(allowedExtensions, ext => ext == extension))
            {
                TempData["ErrorMessage"] = "Solo se permiten imágenes (jpg, jpeg, png, gif)";
                return RedirectToPage();
            }

            if (FotoFile.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "La imagen no puede superar los 5MB";
                return RedirectToPage();
            }

            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsPath);

                if (!string.IsNullOrEmpty(usuario.FotoPerfil))
                {
                    var oldPhotoPath = Path.Combine(_environment.WebRootPath, usuario.FotoPerfil.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhotoPath))
                    {
                        System.IO.File.Delete(oldPhotoPath);
                    }
                }

                var fileName = $"{usuario.UsuarioID}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await FotoFile.CopyToAsync(stream);
                }

                usuario.FotoPerfil = $"/uploads/profiles/{fileName}";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Foto de perfil actualizada correctamente";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile photo");
                TempData["ErrorMessage"] = "Error al subir la foto de perfil";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToPage("/Index");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == userEmail);
            if (usuario == null)
            {
                return NotFound();
            }

            if (ChangePassword == null || !ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Por favor corrige los errores en el formulario";
                return RedirectToPage();
            }

            try
            {
                var currentPasswordHash = HashPassword(ChangePassword.ContrasenaActual);
                if (currentPasswordHash != usuario.ContrasenaHash)
                {
                    TempData["ErrorMessage"] = "La contraseña actual es incorrecta";
                    return RedirectToPage();
                }

                usuario.ContrasenaHash = HashPassword(ChangePassword.NuevaContrasena);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Contraseña cambiada correctamente";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                TempData["ErrorMessage"] = "Error al cambiar la contraseña";
                return RedirectToPage();
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
