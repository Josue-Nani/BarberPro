using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BarberPro.Data;
using BarberPro.Models;
using System.Security.Claims;

namespace BarberPro.Pages
{
    [Authorize]
    public class MisReservasModel : PageModel
    {
        private readonly BarberContext _context;

        public MisReservasModel(BarberContext context)
        {
            _context = context;
        }

        public List<ReservaClienteVM> Reservas { get; set; } = new List<ReservaClienteVM>();
        public string? MensajeError { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Obtener el ID del usuario autenticado
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    MensajeError = "No se pudo identificar al usuario.";
                    return;
                }

                // Buscar el ClienteID asociado a este usuario
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.UsuarioID == userId);

                if (cliente == null)
                {
                    MensajeError = "No se encontró información del cliente.";
                    return;
                }

                // Cargar todas las reservas del cliente con información relacionada
                var reservasData = await _context.Reservas
                    .Where(r => r.ClienteID == cliente.ClienteID)
                    .OrderByDescending(r => r.FechaReserva)
                    .ThenByDescending(r => r.HoraInicio)
                    .ToListAsync();

                // Obtener IDs únicos para cargar datos relacionados
                var barberoIds = reservasData.Select(r => r.BarberoID).Distinct().ToList();
                var servicioIds = reservasData.Select(r => r.ServicioID).Distinct().ToList();

                // Cargar barberos con usuarios
                var barberos = await _context.Barberos
                    .Include(b => b.Usuario)
                    .Where(b => barberoIds.Contains(b.BarberoID))
                    .ToDictionaryAsync(b => b.BarberoID);

                // Cargar servicios
                var servicios = await _context.Servicios
                    .Where(s => servicioIds.Contains(s.ServicioID))
                    .ToDictionaryAsync(s => s.ServicioID);

                // Mapear a ViewModel
                Reservas = reservasData.Select(r => new ReservaClienteVM
                {
                    ReservaID = r.ReservaID,
                    FechaReserva = r.FechaReserva,
                    HoraInicio = r.HoraInicio,
                    HoraFin = r.HoraFin,
                    ServicioNombre = servicios.ContainsKey(r.ServicioID) 
                        ? servicios[r.ServicioID].Nombre 
                        : "Servicio no disponible",
                    ServicioPrecio = servicios.ContainsKey(r.ServicioID) 
                        ? servicios[r.ServicioID].Precio 
                        : 0,
                    BarberoNombre = barberos.ContainsKey(r.BarberoID) && barberos[r.BarberoID].Usuario != null
                        ? barberos[r.BarberoID].Usuario!.NombreCompleto
                        : "Barbero no disponible",
                    Estado = r.Estado ?? "Pendiente",
                    FechaCreacion = r.FechaCreacion
                }).ToList();
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al cargar las reservas: {ex.Message}";
            }
        }
    }

    public class ReservaClienteVM
    {
        public int ReservaID { get; set; }
        public DateTime FechaReserva { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public decimal ServicioPrecio { get; set; }
        public string BarberoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }

        // Display helpers
        public string DisplayFecha => FechaReserva.ToString("dd/MM/yyyy");
        public string DisplayHora => $"{HoraInicio:hh\\:mm} - {HoraFin:hh\\:mm}";
        
        public string EstadoClass => Estado switch
        {
            "Confirmada" => "badge-success",
            "Cancelada" => "badge-error",
            "Completada" => "badge-info",
            _ => "badge-warning"
        };

        public string EstadoIcono => Estado switch
        {
            "Confirmada" => "✓",
            "Cancelada" => "✗",
            "Completada" => "✓✓",
            _ => "⏳"
        };

        public bool EsFutura => FechaReserva.Date >= DateTime.Now.Date;
        public bool EsPasada => FechaReserva.Date < DateTime.Now.Date;
    }
}
