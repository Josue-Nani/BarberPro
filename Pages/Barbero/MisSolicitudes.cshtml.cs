using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BarberPro.Pages.Barbero
{
    [Authorize(Policy = "BarberOrAdminOnly")]
    public class MisSolicitudesModel : PageModel
    {
        private readonly BarberContext _context;

        public MisSolicitudesModel(BarberContext context)
        {
            _context = context;
        }

        public List<SolicitudDisponibilidad> Solicitudes { get; set; } = new();
        public string? MensajeExito { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int usuarioId))
            {
                return;
            }

            var barbero = await _context.Barberos
                .FirstOrDefaultAsync(b => b.UsuarioID == usuarioId);

            if (barbero == null)
            {
                return;
            }

            // Load all solicitudes for this barbero ordered by most recent first
            Solicitudes = await _context.SolicitudesDisponibilidad
                .Where(s => s.BarberoID == barbero.BarberoID)
                .Include(s => s.AdminRespondente)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            // Check for success message
            if (TempData["SuccessMessage"] != null)
            {
                MensajeExito = TempData["SuccessMessage"]?.ToString();
            }
        }
    }
}
