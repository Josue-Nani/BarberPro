using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BarberPro.Data;
using BarberPro.Models;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.Barberos
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly BarberContext _context;

        public EditModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public BarberoModel Barbero { get; set; } = new();

        [BindProperty]
        public List<int> HorariosSeleccionados { get; set; } = new();

        public List<Usuario> Usuarios { get; set; } = new();
        public SelectList? HorariosDisponibles { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Barbero = await _context.Barberos
                .Include(b => b.Usuario)
                .Include(b => b.Horarios)
                .FirstOrDefaultAsync(b => b.BarberoID == id) ?? new BarberoModel();

            if (Barbero.BarberoID == 0) return NotFound();

            Usuarios = await _context.Usuarios
                .Where(u => u.Estado == true)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();

            var horarios = await _context.HorariosBarbero
                .Where(h => h.Disponible && (h.BarberoID == 0 || h.BarberoID == Barbero.BarberoID))
                .OrderBy(h => h.Fecha)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            HorariosDisponibles = new SelectList(horarios, nameof(HorarioBarbero.HorarioID), nameof(HorarioBarbero.DisplayText));
            HorariosSeleccionados = Barbero.Horarios?.Select(h => h.HorarioID).ToList() ?? new List<int>();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(Barbero.BarberoID);
                return Page();
            }

            _context.Attach(Barbero).State = EntityState.Modified;

            var horariosPrevios = await _context.HorariosBarbero
                .Where(h => h.BarberoID == Barbero.BarberoID)
                .ToListAsync();
            
            foreach (var h in horariosPrevios)
            {
                h.BarberoID = 0;
            }

            if (HorariosSeleccionados != null && HorariosSeleccionados.Any())
            {
                var nuevos = await _context.HorariosBarbero
                    .Where(h => HorariosSeleccionados.Contains(h.HorarioID) && h.Disponible)
                    .ToListAsync();
                    
                foreach (var h in nuevos)
                {
                    h.BarberoID = Barbero.BarberoID;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Barbero actualizado exitosamente.";
            return RedirectToPage("./Index");
        }
    }
}
