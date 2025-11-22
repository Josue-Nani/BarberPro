using System.Diagnostics;
using BarberPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BarberPro.Data;
using System.Linq;

namespace BarberPro.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BarberContext _context;

        public HomeController(ILogger<HomeController> logger, BarberContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Nueva acción Inicio
        public IActionResult Inicio()
        {
            // load active services
            var servicios = _context.Servicios.Where(s => s.Estado).ToList();
            return View(servicios);
        }

        // Cerrar sesión: limpiar session y cookie de autenticación
        public async Task<IActionResult> Salir()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Login");
        }

        // Acción protegida: la autorización se maneja con [Authorize] a nivel de controlador
        public IActionResult Contactos()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
