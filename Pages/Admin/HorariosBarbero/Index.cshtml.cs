using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;

namespace BarberPro.Pages.Admin.HorariosBarbero
{
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;

        public IndexModel(BarberContext context)
        {
            _context = context;
        }

        public List<HorarioBarbero> Horarios { get; set; } = new();

        public async Task OnGetAsync()
        {
            Horarios = await _context.HorariosBarbero
                .Include(h => h.Barbero)
                    .ThenInclude(b => b.Usuario)
                .OrderBy(h => h.Fecha)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();
        }
    }
}
