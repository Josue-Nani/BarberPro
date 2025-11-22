using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Pages.Admin.Reservas
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;
        public IndexModel(BarberContext context) => _context = context;

        public List<Reserva> Reservas { get; set; }

        public void OnGet()
        {
            Reservas = _context.Reservas
                               .OrderByDescending(r => r.FechaCreacion)
                               .ToList();
        }
    }
}