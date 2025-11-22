using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace BarberPro.Pages.Admin.Servicios
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;
        public IndexModel(BarberContext context) => _context = context;

        public List<Servicio> Servicios { get; set; }

        public void OnGet()
        {
            Servicios = _context.Servicios.ToList();
        }
    }
}