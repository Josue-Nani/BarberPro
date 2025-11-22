using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Pages.Admin.Barberos
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;
        public IndexModel(BarberContext context) => _context = context;

        public List<Barbero> Barberos { get; set; }

        public void OnGet()
        {
            Barberos = _context.Barberos.Include(b => b.Usuario).ToList();
        }
    }
}