using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BarberPro.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class NotificacionesModel : PageModel
    {
        private readonly BarberContext _context;

        public NotificacionesModel(BarberContext context)
        {
            _context = context;
        }

        public List<SolicitudDisponibilidad> SolicitudesPendientes { get; set; } = new();
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            // Load all pending solicitudes
            SolicitudesPendientes = await _context.SolicitudesDisponibilidad
                .Where(s => s.Estado == "Pendiente")
                .Include(s => s.Barbero)
                    .ThenInclude(b => b!.Usuario)
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            // Check for messages
            if (TempData["SuccessMessage"] != null)
            {
                MensajeExito = TempData["SuccessMessage"]?.ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                MensajeError = TempData["ErrorMessage"]?.ToString();
            }
        }

        public async Task<IActionResult> OnPostAprobarAsync(int solicitudId)
        {
            var solicitud = await _context.SolicitudesDisponibilidad
                .Include(s => s.Barbero)
                .FirstOrDefaultAsync(s => s.SolicitudID == solicitudId);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "Solicitud no encontrada.";
                return RedirectToPage();
            }

            if (solicitud.Estado != "Pendiente")
            {
                TempData["ErrorMessage"] = "Esta solicitud ya fue procesada.";
                return RedirectToPage();
            }

            // Get current admin user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int adminId))
            {
                TempData["ErrorMessage"] = "No se pudo identificar al administrador.";
                return RedirectToPage();
            }

            // Update solicitud
            solicitud.Estado = "Aprobada";
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.AdminRespondenteID = adminId;

            // Update barbero availability
            if (solicitud.Barbero != null)
            {
                solicitud.Barbero.Disponibilidad = "No Disponible";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Solicitud aprobada exitosamente. El barbero ahora está marcado como No Disponible.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRechazarAsync(int solicitudId, string motivoRechazo)
        {
            var solicitud = await _context.SolicitudesDisponibilidad
                .FirstOrDefaultAsync(s => s.SolicitudID == solicitudId);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "Solicitud no encontrada.";
                return RedirectToPage();
            }

            if (solicitud.Estado != "Pendiente")
            {
                TempData["ErrorMessage"] = "Esta solicitud ya fue procesada.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(motivoRechazo))
            {
                TempData["ErrorMessage"] = "Debes proporcionar un motivo para el rechazo.";
                return RedirectToPage();
            }

            // Get current admin user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int adminId))
            {
                TempData["ErrorMessage"] = "No se pudo identificar al administrador.";
                return RedirectToPage();
            }

            // Update solicitud
            solicitud.Estado = "Rechazada";
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.AdminRespondenteID = adminId;
            solicitud.MotivoRechazo = motivoRechazo;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Solicitud rechazada exitosamente.";
            return RedirectToPage();
        }
    }
}
