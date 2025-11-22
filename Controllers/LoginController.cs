using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarberPro.Data;
using BarberPro.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BarberPro.Controllers;

[AllowAnonymous]
public class LoginController : Controller
{
    private readonly BarberContext _context;
    private readonly ILogger<LoginController> _logger;

    public LoginController(BarberContext context, ILogger<LoginController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string correo, string contrasena, string returnUrl = null)
    {
        try
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena))
            {
                ViewBag.Error = "Correo y contraseña son obligatorios";
                return View();
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo && u.Estado == true);
            if (user == null)
            {
                ViewBag.Error = "Usuario no encontrado o inactivo";
                return View();
            }

            var hash = ComputeSha256Hash(contrasena);
            // compare case-insensitive to tolerate hex case differences
            if (!string.Equals(user.ContrasenaHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Contraseña incorrecta";
                _logger.LogWarning("Login failed for {Email}: incorrect password", correo);
                return View();
            }

            // get role name
            string roleName = null;
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RolID == user.RolID);
                if (role != null) roleName = role.NombreRol;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching role for user {UserId}", user.UsuarioID);
                roleName = null;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuarioID.ToString()),
                new Claim(ClaimTypes.Name, user.NombreCompleto),
                new Claim(ClaimTypes.Email, user.Correo)
            };

            if (!string.IsNullOrEmpty(roleName))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            _logger.LogInformation("User {UserId} logged in with role {Role}", user.UsuarioID, roleName);

            // If returnUrl is provided and local, redirect there
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // If user is admin, go to admin users page
            if (!string.IsNullOrEmpty(roleName) && roleName.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect("/Admin/Usuarios");
            }

            // If user is barber, go to admin reservations page
            if (!string.IsNullOrEmpty(roleName) && roleName.Equals("Barbero", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect("/Admin/Reservas/Index");
            }

            return RedirectToAction("Inicio", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during login for {Email}", correo);
            ViewBag.Error = "Error al iniciar sesión";
            return View();
        }
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string nombreCompleto, string correo, string contrasena, string telefono)
    {
        // Trim incoming values
        nombreCompleto = nombreCompleto?.Trim();
        correo = correo?.Trim();
        contrasena = contrasena?.Trim();
        telefono = telefono?.Trim();

        // If binding left them empty, try to read raw form values for diagnosis
        var form = HttpContext.Request?.Form;
        if ((string.IsNullOrEmpty(nombreCompleto) || string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena)) && form != null)
        {
            if (string.IsNullOrEmpty(nombreCompleto) && form.TryGetValue("nombreCompleto", out var fNombre)) nombreCompleto = fNombre.ToString().Trim();
            if (string.IsNullOrEmpty(correo) && form.TryGetValue("correo", out var fCorreo)) correo = fCorreo.ToString().Trim();
            if (string.IsNullOrEmpty(contrasena) && form.TryGetValue("contrasena", out var fCon)) contrasena = fCon.ToString();
            if (string.IsNullOrEmpty(telefono) && form.TryGetValue("telefono", out var fTel)) telefono = fTel.ToString().Trim();

            // Log the raw form keys for debugging
            var keys = string.Join(",", form.Keys);
            _logger.LogInformation("Register POST raw form keys: {Keys}", keys);
        }

        if (string.IsNullOrEmpty(nombreCompleto) || string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena))
        {
            ViewBag.Error = "Todos los campos son obligatorios";
            // Provide diagnostic info for debugging
            ViewBag.DebugInfo = new { nombreCompleto, correo, contrasenaPresent = !string.IsNullOrEmpty(contrasena), telefono };
            return View();
        }

        var exists = await _context.Usuarios.AnyAsync(u => u.Correo == correo);
        if (exists)
        {
            ViewBag.Error = "Ya existe un usuario con ese correo";
            return View();
        }

        var clienteRole = await _context.Roles.FirstOrDefaultAsync(r => r.NombreRol == "Cliente");
        if (clienteRole == null)
        {
            clienteRole = new Rol { NombreRol = "Cliente" };
            _context.Roles.Add(clienteRole);
            await _context.SaveChangesAsync();
        }

        var user = new Usuario
        {
            NombreCompleto = nombreCompleto,
            Correo = correo,
            ContrasenaHash = ComputeSha256Hash(contrasena),
            FechaRegistro = DateTime.UtcNow,
            Estado = true,
            RolID = clienteRole.RolID,
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono
        };

        _context.Usuarios.Add(user);
        await _context.SaveChangesAsync();

        // Si el usuario tiene rol Cliente, crear también el registro en Clientes
        if (clienteRole != null && string.Equals(clienteRole.NombreRol, "Cliente", StringComparison.OrdinalIgnoreCase))
        {
            var cliente = new Cliente
            {
                UsuarioID = user.UsuarioID,
                Direccion = null,
                FechaNacimiento = null
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Login");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
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
