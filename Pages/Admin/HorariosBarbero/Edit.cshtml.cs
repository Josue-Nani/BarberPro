using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.HorariosBarbero
{
    public class EditModel : PageModel
    {
        private readonly BarberContext _context;
        

        public EditModel(BarberContext context)
        {
            _context = context;
            
        }

        [BindProperty]
        public HorarioBarbero Horario { get; set; } = new();

        public List<BarberoModel> Barberos { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Horario = await _context.HorariosBarbero.FindAsync(id);

            if (Horario == null)
            {
                return NotFound();
            }

            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            // Validate time range
            if (Horario.HoraFin <= Horario.HoraInicio)
            {
                ModelState.AddModelError("Horario.HoraFin", "La hora de fin debe ser posterior a la hora de inicio");
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            // Validate FechaFin if provided
            if (Horario.FechaFin.HasValue && Horario.Fecha.HasValue && Horario.FechaFin.Value.Date < Horario.Fecha.Value.Date)
            {
                ModelState.AddModelError("Horario.FechaFin", "La fecha fin debe ser igual o posterior a la fecha inicio");
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            // If single-day schedule, ensure the day isn't configured as day-off for this barbero
            if (!Horario.FechaFin.HasValue && Horario.Fecha.HasValue)
            {
                // DisponibilidadService removed
            }

            // Check for overlapping schedules (exclude current Horario)
            var startDate = Horario.Fecha.Value.Date;
            var endDate = (Horario.FechaFin ?? Horario.Fecha).Value.Date;

            var overlapping = await _context.HorariosBarbero
                .Where(h => h.BarberoID == Horario.BarberoID && h.HorarioID != Horario.HorarioID && h.Fecha.HasValue)
                .AnyAsync(h =>
                    // compare ranges: h.Fecha .. (h.FechaFin ?? h.Fecha)  vs startDate..endDate
                    (h.Fecha.Value.Date <= endDate) && ((h.FechaFin ?? h.Fecha).Value.Date >= startDate)
                );

            if (overlapping)
            {
                ModelState.AddModelError("", "Ya existe un horario que se solapa con este en la misma fecha");
                Barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .ToListAsync();
                return Page();
            }

            _context.Attach(Horario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HorarioExists(Horario.HorarioID))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task<bool> HorarioExists(int id)
        {
            return await _context.HorariosBarbero.AnyAsync(e => e.HorarioID == id);
        }
    }
}


