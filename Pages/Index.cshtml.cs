using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;

        public IndexModel(BarberContext context)
        {
            _context = context;
        }

        public List<Servicio> Servicios { get; set; } = new List<Servicio>();
        public List<BarberoModel> Barberos { get; set; } = new List<BarberoModel>();

        public async Task OnGetAsync()
        {
            // Cargar servicios activos
            Servicios = await _context.Servicios
                .Where(s => s.Estado)
                .OrderBy(s => s.Nombre)
                .ToListAsync();

            // Cargar barberos
            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => b.Usuario != null)
                .Take(3)
                .ToListAsync();
        }
    }
}
