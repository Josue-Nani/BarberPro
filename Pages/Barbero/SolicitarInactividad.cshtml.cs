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
    [Authorize(Policy = "BarberOrAdmin")]
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
            Console.WriteLine("=== OnPostAsync CALLED ===");
            Console.WriteLine($"FechaInicio: {Solicitud.FechaInicio}");
            Console.WriteLine($"FechaFin: {Solicitud.FechaFin}");
            Console.WriteLine($"Motivo: {Solicitud.Motivo}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                MensajeError = "Por favor completa todos los campos correctamente. ";
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    MensajeError += error.ErrorMessage + " ";
                    Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }
                return Page();
            }

            var barbero = await GetBarberoActualAsync();
            if (barbero == null)
            {
                Console.WriteLine("ERROR: Barbero not found");
                MensajeError = "No se pudo identificar al barbero.";
                return Page();
            }

            Console.WriteLine($"Barbero found: ID={barbero.BarberoID}, Nombre={barbero.Usuario?.NombreCompleto}");

            // Validations
            if (Solicitud.FechaInicio < DateTime.Today)
            {
                Console.WriteLine("ERROR: FechaInicio in the past");
                MensajeError = "La fecha de inicio debe ser hoy o en el futuro.";
                return Page();
            }

            if (Solicitud.FechaFin < Solicitud.FechaInicio)
            {
                Console.WriteLine("ERROR: FechaFin before FechaInicio");
                MensajeError = "La fecha de fin debe ser posterior o igual a la fecha de inicio.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Solicitud.Motivo) || Solicitud.Motivo.Length < 10)
            {
                Console.WriteLine($"ERROR: Motivo too short. Length={Solicitud.Motivo?.Length ?? 0}");
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
                Console.WriteLine("ERROR: Conflicting pending request");
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

            Console.WriteLine("Adding solicitud to context...");
            _context.SolicitudesDisponibilidad.Add(nuevaSolicitud);
            
            Console.WriteLine("Saving changes...");
            await _context.SaveChangesAsync();
            Console.WriteLine("Changes saved successfully!");

            TempData["SuccessMessage"] = "Solicitud enviada exitosamente. El administrador la revisará pronto.";
            Console.WriteLine("Redirecting to MisSolicitudes...");
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
