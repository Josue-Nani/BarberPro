using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;

namespace BarberPro.Pages.Admin.Servicios
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateModel : PageModel
    {
        private readonly BarberContext _context;
        public CreateModel(BarberContext context) => _context = context;

        [BindProperty]
        public Servicio Servicio { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();
            _context.Servicios.Add(Servicio);
            _context.SaveChanges();
            return RedirectToPage("/Admin/Servicios/Index");
        }
    }
}