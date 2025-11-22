using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberoModel = BarberPro.Models.Barbero;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BarberPro.Models;

namespace BarberPro.Pages.Admin.Barberos
{
    [Authorize(Policy = "BarberOrAdmin")]
    public class CambiarDisponibilidadModel : PageModel
    {
        private readonly BarberContext _context;

        public CambiarDisponibilidadModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public BarberoModel? Barbero { get; set; }

        // Motivo and dates for requesting unavailability
        [BindProperty]
        public string? Motivo { get; set; }

        [BindProperty]
        public DateTime? FechaInicio { get; set; }

        [BindProperty]
        public DateTime? FechaFin { get; set; }

        public bool IsBarber { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadBarbero();
            if (Barbero == null)
            {
                return NotFound("No se encontró información de barbero para este usuario.");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadBarbero();
                return Page();
            }

            // Guardar el valor de disponibilidad seleccionado antes de llamar a LoadBarbero
            var disponibilidadSeleccionada = Barbero?.Disponibilidad;
            var barberoIdFormulario = Barbero?.BarberoID;

            if (barberoIdFormulario == null || barberoIdFormulario == 0)
            {
                return NotFound();
            }

            // VALIDACIÓN DE SEGURIDAD: Verificar que el usuario autenticado sea dueño de este barbero
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Forbid();
            }

            var barberoDb = await _context.Barberos
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.BarberoID == barberoIdFormulario);

            if (barberoDb == null)
            {
                return NotFound("Barbero no encontrado.");
            }

            // Verificar que el usuario autenticado es el dueño del barbero
            if (barberoDb.UsuarioID != userId)
            {
                Console.WriteLine($"SECURITY VIOLATION: User {userId} intentó modificar barbero {barberoIdFormulario} (dueño: {barberoDb.UsuarioID})");
                return Forbid("No tienes permiso para modificar este barbero.");
            }

            if (string.IsNullOrEmpty(disponibilidadSeleccionada))
            {
                await LoadBarbero();
                ModelState.AddModelError(string.Empty, "Selecciona un estado de disponibilidad.");
                return Page();
            }

            // If the barber selected 'No Disponible' -> create a SolicitudDisponibilidad instead of changing immediately
            if (disponibilidadSeleccionada == "No Disponible")
            {
                // Validate Motivo
                if (string.IsNullOrWhiteSpace(Motivo) || Motivo.Length < 10)
                {
                    await LoadBarbero();
                    ModelState.AddModelError(nameof(Motivo), "El motivo debe tener al menos 10 caracteres.");
                    return Page();
                }

                // Validate dates
                if (!FechaInicio.HasValue || !FechaFin.HasValue)
                {
                    await LoadBarbero();
                    ModelState.AddModelError(string.Empty, "Debes especificar Fecha Inicio y Fecha Fin para la solicitud.");
                    return Page();
                }

                if (FechaInicio.Value.Date < DateTime.Today || FechaFin.Value.Date < FechaInicio.Value.Date)
                {
                    await LoadBarbero();
                    ModelState.AddModelError(string.Empty, "Fechas inválidas. Fecha inicio debe ser hoy o después, y Fecha fin debe ser igual o posterior a Fecha inicio.");
                    return Page();
                }

                // Check for conflicting pending requests
                var conflictoPendiente = await _context.SolicitudesDisponibilidad
                    .AnyAsync(s => s.BarberoID == barberoDb.BarberoID && s.Estado == "Pendiente" && ((s.FechaInicio <= FechaFin && s.FechaFin >= FechaInicio)));

                if (conflictoPendiente)
                {
                    TempData["Mensaje"] = "Ya tienes una solicitud pendiente para estas fechas.";
                    return RedirectToPage();
                }

                var solicitud = new SolicitudDisponibilidad
                {
                    BarberoID = barberoDb.BarberoID,
                    FechaInicio = FechaInicio.Value.Date,
                    FechaFin = FechaFin.Value.Date,
                    Motivo = Motivo,
                    Estado = "Pendiente",
                    FechaSolicitud = DateTime.Now
                };

                _context.SolicitudesDisponibilidad.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Solicitud enviada al administrador. Solo será aplicada si es aprobada.";
                return RedirectToPage();
            }

            // For other statuses (Disponible, Ocupado) update immediately
            Console.WriteLine($"Actualizando disponibilidad de Barbero {barberoDb.BarberoID} (Usuario {userId}): {barberoDb.Disponibilidad} -> {disponibilidadSeleccionada}");
            barberoDb.Disponibilidad = disponibilidadSeleccionada;
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = "Disponibilidad actualizada correctamente.";

            return RedirectToPage();
        }

        private async Task LoadBarbero()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var user = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.UsuarioID == userId);
                if (user != null && user.Rol != null && user.Rol.NombreRol == "Barbero")
                {
                    IsBarber = true;
                    Barbero = await _context.Barberos
                        .Include(b => b.Usuario)
                        .FirstOrDefaultAsync(b => b.UsuarioID == userId);
                }
                else if (user != null && user.Rol != null && user.Rol.NombreRol == "Administrador")
                {
                    // Administradores pueden ver su propio registro si existe
                    Barbero = await _context.Barberos
                        .Include(b => b.Usuario)
                        .FirstOrDefaultAsync(b => b.UsuarioID == userId);
                }
            }
        }
    }
}
