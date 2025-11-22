using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Barbero
{
    [Authorize(Policy = "BarberOrAdminOnly")]
    public class SolicitarInactividadModel : PageModel
    {
        private readonly BarberContext _context;

        public SolicitarInactividadModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SolicitudDisponibilidad Solicitud { get; set; } = new();

        public string? MensajeError { get; set; }
        public string? MensajeExito { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verify user is a barber
            var barbero = await GetBarberoActualAsync();
            if (barbero == null)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var barbero = await GetBarberoActualAsync();
            if (barbero == null)
            {
                MensajeError = "No se pudo identificar al barbero.";
                return Page();
            }

            // Validations
            if (Solicitud.FechaInicio < DateTime.Today)
            {
                MensajeError = "La fecha de inicio debe ser hoy o en el futuro.";
                return Page();
            }

            if (Solicitud.FechaFin < Solicitud.FechaInicio)
            {
                MensajeError = "La fecha de fin debe ser posterior o igual a la fecha de inicio.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Solicitud.Motivo) || Solicitud.Motivo.Length < 10)
            {
                MensajeError = "El motivo debe tener al menos 10 caracteres.";
                return Page();
            }

            // Check for conflicting pending requests
            var conflictoPendiente = await _context.SolicitudesDisponibilidad
                .AnyAsync(s => s.BarberoID == barbero.BarberoID &&
                               s.Estado == "Pendiente" &&
                               ((s.FechaInicio <= Solicitud.FechaFin && s.FechaFin >= Solicitud.FechaInicio)));

            if (conflictoPendiente)
            {
                MensajeError = "Ya tienes una solicitud pendiente para estas fechas.";
                return Page();
            }

            // Create solicitud
            var nuevaSolicitud = new SolicitudDisponibilidad
            {
                BarberoID = barbero.BarberoID,
                FechaInicio = Solicitud.FechaInicio,
                FechaFin = Solicitud.FechaFin,
                Motivo = Solicitud.Motivo,
                Estado = "Pendiente",
                FechaSolicitud = DateTime.Now
            };

            _context.SolicitudesDisponibilidad.Add(nuevaSolicitud);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Solicitud enviada exitosamente. El administrador la revisará pronto.";
            return RedirectToPage("/Barbero/MisSolicitudes");
        }

        private async Task<BarberoModel?> GetBarberoActualAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int usuarioId))
                return null;

            return await _context.Barberos
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.UsuarioID == usuarioId);
        }
    }
}
