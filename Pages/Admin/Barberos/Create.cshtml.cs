using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberPro.Pages.Admin.Barberos
{
    [Authorize(Policy = "AdminOnly")]
    public class CreateModel : PageModel
    {
        private readonly BarberContext _context;

        public CreateModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Barbero Barbero { get; set; } = new();

        // Selected schedule IDs from the multi‑select
        [BindProperty]
        public List<int> HorariosSeleccionados { get; set; } = new();

        public List<Usuario> Usuarios { get; set; } = new();
        public SelectList? HorariosDisponibles { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Usuarios que todavía no son barberos
            var usuariosConBarbero = await _context.Barberos.Select(b => b.UsuarioID).ToListAsync();
            Usuarios = await _context.Usuarios
                .Where(u => u.Estado == true && !usuariosConBarbero.Contains(u.UsuarioID))
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();

            // Horarios libres (Disponible = true y sin barbero asignado)
            var horarios = await _context.HorariosBarbero
                .Where(h => h.Disponible && h.BarberoID == 0)
                .OrderBy(h => h.Fecha)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            HorariosDisponibles = new SelectList(horarios, nameof(HorarioBarbero.HorarioID), nameof(HorarioBarbero.DisplayText));
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Recargar listas para volver a mostrar el formulario
                await OnGetAsync();
                return Page();
            }

            // Guardar el nuevo barbero
            _context.Barberos.Add(Barbero);
            await _context.SaveChangesAsync();

            // Asignar los horarios seleccionados al barbero recién creado
            if (HorariosSeleccionados != null && HorariosSeleccionados.Any())
            {
                var horarios = await _context.HorariosBarbero
                    .Where(h => HorariosSeleccionados.Contains(h.HorarioID) && h.Disponible)
                    .ToListAsync();

                foreach (var h in horarios)
                {
                    h.BarberoID = Barbero.BarberoID;
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Barbero creado exitosamente.";
            return RedirectToPage("./Index");
        }
    }
}
