using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace BarberPro.Pages.Admin.Servicios
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly BarberContext _context;
        public EditModel(BarberContext context) => _context = context;

        [BindProperty]
        public Servicio Servicio { get; set; }

        public IActionResult OnGet(int id)
        {
            Servicio = _context.Servicios.FirstOrDefault(s => s.ServicioID == id);
            if (Servicio == null) return RedirectToPage("/Admin/Servicios/Index");
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();
            _context.Servicios.Update(Servicio);
            _context.SaveChanges();
            return RedirectToPage("/Admin/Servicios/Index");
        }
    }
}