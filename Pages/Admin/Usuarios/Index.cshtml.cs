using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Pages.Admin.Usuarios
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;
        public IndexModel(BarberContext context) => _context = context;

        public List<Usuario> Usuarios { get; set; }

        public void OnGet()
        {
            Usuarios = _context.Usuarios.Include(u => u.Rol).ToList();
        }
    }
}