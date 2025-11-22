using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.Barberos
{
    [Authorize(Policy = "AdminOnly")]
    public class DeleteModel : PageModel
    {
        private readonly BarberContext _context;
        public DeleteModel(BarberContext context) => _context = context;

        [BindProperty]
        public BarberoModel Barbero { get; set; }

        public IActionResult OnGet(int id)
        {
            Barbero = _context.Barberos.Include(b => b.Usuario).FirstOrDefault(b => b.BarberoID == id);
            if (Barbero == null) return RedirectToPage("/Admin/Barberos/Index");
            return Page();
        }

        public IActionResult OnPost()
        {
            var b = _context.Barberos.FirstOrDefault(x => x.BarberoID == Barbero.BarberoID);
            if (b != null)
            {
                _context.Barberos.Remove(b);
                _context.SaveChanges();
            }
            return RedirectToPage("/Admin/Barberos/Index");
        }
    }
}