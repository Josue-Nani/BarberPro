using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using System.Linq;

namespace BarberPro.Pages.Api
{
    public class GetBarberosModel : PageModel
    {
        private readonly BarberContext _context;

        public GetBarberosModel(BarberContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int? servicioId)
        {
            // Get all active barberos with their user info
            var barberos = _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => b.Usuario != null)
                .Select(b => new
                {
                    barberoID = b.BarberoID,
                    nombre = b.Usuario!.NombreCompleto,
                    especialidades = b.Especialidades,
                    fotoPerfil = b.Usuario.FotoPerfil
                })
                .ToList();

            return new JsonResult(barberos);
        }
    }
}
