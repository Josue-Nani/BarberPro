using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.Reservas
{
    [Authorize(Policy = "BarberOrAdmin")]
    public class IndexModel : PageModel
    {
        private readonly BarberContext _context;

        public IndexModel(BarberContext context)
        {
            _context = context;
        }

        // Indica si el usuario actual es un barbero
        public bool IsBarber { get; set; }
        public int? CurrentBarberID { get; set; }

        // Filtros
        [BindProperty(SupportsGet = true)]
        public DateTime? FechaInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FechaFin { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BarberoID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ServicioID { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? BusquedaCliente { get; set; }

        // Listas para dropdowns
        public List<BarberoModel> Barberos { get; set; } = new List<BarberoModel>();
        public List<Servicio> Servicios { get; set; } = new List<Servicio>();

        // Reservas
        public List<ReservaViewModel> Reservas { get; set; } = new List<ReservaViewModel>();

        public async Task OnGetAsync()
        {
            // Detectar si el usuario es barbero
            await DetectUserRole();

            // Cargar listas para filtros
            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => b.Usuario != null)
                .ToListAsync();

            Servicios = await _context.Servicios
                .Where(s => s.Estado)
                .ToListAsync();

            // Construir query con filtros
            var query = _context.Reservas.AsQueryable();

            // Si es barbero, filtrar solo sus reservas
            if (IsBarber && CurrentBarberID.HasValue)
            {
                query = query.Where(r => r.BarberoID == CurrentBarberID.Value);
            }

            // Aplicar filtros
            if (FechaInicio.HasValue)
            {
                query = query.Where(r => r.FechaReserva >= FechaInicio.Value.Date);
            }

            if (FechaFin.HasValue)
            {
                query = query.Where(r => r.FechaReserva <= FechaFin.Value.Date);
            }

            // Solo permitir filtro de barbero si es administrador
            if (!IsBarber && BarberoID.HasValue && BarberoID.Value > 0)
            {
                query = query.Where(r => r.BarberoID == BarberoID.Value);
            }

            if (ServicioID.HasValue && ServicioID.Value > 0)
            {
                query = query.Where(r => r.ServicioID == ServicioID.Value);
            }

            if (!string.IsNullOrEmpty(Estado))
            {
                query = query.Where(r => r.Estado == Estado);
            }

            // Cargar reservas
            var reservasData = await query
                .OrderByDescending(r => r.FechaReserva)
                .ThenByDescending(r => r.HoraInicio)
                .ToListAsync();

            // Cargar relaciones separadamente
            var clienteIds = reservasData.Select(r => r.ClienteID).Distinct().ToList();
            var barberoIds = reservasData.Select(r => r.BarberoID).Distinct().ToList();
            var servicioIds = reservasData.Select(r => r.ServicioID).Distinct().ToList();

            var clientes = await _context.Clientes
                .Include(c => c.Usuario)
                .Where(c => clienteIds.Contains(c.ClienteID))
                .ToDictionaryAsync(c => c.ClienteID);

            var barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => barberoIds.Contains(b.BarberoID))
                .ToDictionaryAsync(b => b.BarberoID);

            var servicios = await _context.Servicios
                .Where(s => servicioIds.Contains(s.ServicioID))
                .ToDictionaryAsync(s => s.ServicioID);

            // Mapear a ViewModel
            Reservas = reservasData.Select(r => new ReservaViewModel
            {
                ReservaID = r.ReservaID,
                FechaReserva = r.FechaReserva,
                HoraInicio = r.HoraInicio,
                HoraFin = r.HoraFin,
                ClienteNombre = clientes.ContainsKey(r.ClienteID) && clientes[r.ClienteID].Usuario != null
                    ? clientes[r.ClienteID].Usuario!.NombreCompleto
                    : "Cliente #" + r.ClienteID,
                ClienteCorreo = clientes.ContainsKey(r.ClienteID) && clientes[r.ClienteID].Usuario != null
                    ? clientes[r.ClienteID].Usuario!.Correo
                    : "",
                BarberoNombre = barberos.ContainsKey(r.BarberoID) && barberos[r.BarberoID].Usuario != null
                    ? barberos[r.BarberoID].Usuario!.NombreCompleto
                    : "Barbero #" + r.BarberoID,
                ServicioNombre = servicios.ContainsKey(r.ServicioID)
                    ? servicios[r.ServicioID].Nombre
                    : "Servicio #" + r.ServicioID,
                ServicioPrecio = servicios.ContainsKey(r.ServicioID)
                    ? servicios[r.ServicioID].Precio
                    : 0,
                Estado = r.Estado ?? "Pendiente",
                FechaCreacion = r.FechaCreacion
            }).ToList();

            // Filtrar por búsqueda de cliente si se proporcionó
            if (!string.IsNullOrEmpty(BusquedaCliente))
            {
                var busqueda = BusquedaCliente.ToLower();
                Reservas = Reservas.Where(r =>
                    r.ClienteNombre.ToLower().Contains(busqueda) ||
                    r.ClienteCorreo.ToLower().Contains(busqueda)
                ).ToList();
            }
        }

        private async Task DetectUserRole()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var user = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.UsuarioID == userId);
                if (user != null && user.Rol != null && user.Rol.NombreRol == "Barbero")
                {
                    IsBarber = true;
                    // Buscar el barbero asociado a este usuario
                    var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.UsuarioID == userId);
                    CurrentBarberID = barbero?.BarberoID;
                }
            }
        }

        public async Task<IActionResult> OnPostCambiarEstadoAsync(int reservaId, string nuevoEstado)
        {
            var reserva = await _context.Reservas.FindAsync(reservaId);
            if (reserva == null)
            {
                return NotFound();
            }

            // Si es barbero, validar que solo pueda cambiar el estado de sus propias reservas
            if (IsBarber || User.IsInRole("Barbero"))
            {
                await DetectUserRole();
                if (CurrentBarberID.HasValue && reserva.BarberoID != CurrentBarberID.Value)
                {
                    return Forbid(); // No puede modificar reservas de otros barberos
                }
            }

            reserva.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }

    public class ReservaViewModel
    {
        public int ReservaID { get; set; }
        public DateTime FechaReserva { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteCorreo { get; set; } = string.Empty;
        public string BarberoNombre { get; set; } = string.Empty;
        public string ServicioNombre { get; set; } = string.Empty;
        public decimal ServicioPrecio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }

        public string DisplayFecha => FechaReserva.ToString("dd/MM/yyyy");
        public string DisplayHora => $"{HoraInicio:hh\\:mm} - {HoraFin:hh\\:mm}";
        public string EstadoClass => Estado switch
        {
            "Confirmada" => "bg-green-600",
            "Cancelada" => "bg-red-600",
            "Completada" => "bg-blue-600",
            _ => "bg-yellow-600"
        };
    }
}