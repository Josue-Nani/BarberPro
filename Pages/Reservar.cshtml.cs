using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using BarberPro.Data;
using BarberPro.Models;

namespace BarberPro.Pages
{
    public class ReservarModel : PageModel
    {
        private readonly BarberContext _context;

        public ReservarModel(BarberContext context)
        {
            _context = context;
        }

        public List<Servicio> Servicios { get; set; } = new List<Servicio>();
        public string? Mensaje { get; set; }

        public void OnGet()
        {
            // Load only active services for initial page load
            Servicios = _context.Servicios.Where(s => s.Estado).ToList();
            
            // Note: TempData["SuccessMessage"] is displayed directly in the view
            // Don't assign it to Mensaje to avoid duplicate messages
        }

        // Accept Fecha as string to avoid binding issues and parse manually
        public async Task<IActionResult> OnPostAsync(int ServicioID, int BarberoID, string Fecha, int HorarioID, string HoraInicio, string HoraFin)
        {
            // Diagnostic: read raw form values for debugging
            var form = Request?.Form;

            // Safely extract form values without risking NullReferenceException
            var fServicio = string.Empty;
            var fBarbero = string.Empty;
            var fFecha = string.Empty;
            var fHorario = string.Empty;
            var fHoraInicio = string.Empty;
            var fHoraFin = string.Empty;

            if (form != null)
            {
                if (form.TryGetValue("ServicioID", out var svc)) fServicio = svc.ToString();
                if (form.TryGetValue("BarberoID", out var brb)) fBarbero = brb.ToString();
                if (form.TryGetValue("Fecha", out var fdt)) fFecha = fdt.ToString();
                if (form.TryGetValue("HorarioID", out var hrd)) fHorario = hrd.ToString();
                if (form.TryGetValue("HoraInicio", out var hin)) fHoraInicio = hin.ToString();
                if (form.TryGetValue("HoraFin", out var hfn)) fHoraFin = hfn.ToString();
            }

            Console.WriteLine("--- Reservar POST received raw form values ---");
            Console.WriteLine($"form ServicioID='{fServicio}', ServicioID(param)='{ServicioID}'");
            Console.WriteLine($"form BarberoID='{fBarbero}', BarberoID(param)='{BarberoID}'");
            Console.WriteLine($"form Fecha='{fFecha}', Fecha(param)='{Fecha}'");
            Console.WriteLine($"form HorarioID='{fHorario}', HorarioID(param)='{HorarioID}'");
            Console.WriteLine($"form HoraInicio='{fHoraInicio}', HoraInicio(param)='{HoraInicio}'");
            Console.WriteLine($"form HoraFin='{fHoraFin}', HoraFin(param)='{HoraFin}'");

            Console.WriteLine($"User.Identity.Name = '{User?.Identity?.Name}'");

            Console.WriteLine($"=== OnPostAsync CALLED ===");
            Console.WriteLine($"ServicioID: {ServicioID}, BarberoID: {BarberoID}, Fecha: {Fecha}, HorarioID: {HorarioID}, HoraInicio: {HoraInicio}, HoraFin: {HoraFin}");
            
            // Basic presence validation: require servicio, barbero and fecha. For times allow either HorarioID provided or explicit HoraInicio/HoraFin.
            if (ServicioID == 0 || BarberoID == 0 || string.IsNullOrEmpty(Fecha) || (HorarioID == 0 && (string.IsNullOrEmpty(HoraInicio) || string.IsNullOrEmpty(HoraFin))))
            {
                Console.WriteLine("VALIDATION FAILED - Missing required fields. Values:\n" +
                    $"ServicioID={ServicioID}, BarberoID={BarberoID}, Fecha='{Fecha}', HorarioID={HorarioID}, HoraInicio='{HoraInicio}', HoraFin='{HoraFin}'");

                // Include received raw form values to help diagnose
                Mensaje = "Debes completar todos los pasos. Valores recibidos: " +
                    $"svc='{fServicio}', bar='{fBarbero}', fecha='{fFecha}', hor='{fHorario}', hin='{fHoraInicio}', hfin='{fHoraFin}'";

                OnGet();
                return Page();
            }

            // Parse Fecha
            if (!DateTime.TryParse(Fecha, out var parsedFecha))
            {
                Console.WriteLine($"Failed to parse Fecha: {Fecha}");
                Mensaje = "Fecha inválida.";
                OnGet();
                return Page();
            }

            // Get the servicio early so we can compute expected duration if needed
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.ServicioID == ServicioID);
            if (servicio == null)
            {
                Console.WriteLine("SERVICIO NOT FOUND in database");
                Mensaje = "Servicio no encontrado.";
                OnGet();
                return Page();
            }
            var expectedDuration = TimeSpan.FromMinutes(servicio.DuracionMinutos);

            // ====== VALIDACIÓN DE DISPONIBILIDAD DEL BARBERO ======
            var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.BarberoID == BarberoID);
            if (barbero == null)
            {
                Console.WriteLine("BARBERO NOT FOUND in database");
                Mensaje = "Barbero no encontrado.";
                OnGet();
                return Page();
            }

            // Verificar que el barbero esté disponible
            var disponibilidad = barbero.Disponibilidad ?? "Disponible";
            if (disponibilidad != "Disponible")
            {
                Console.WriteLine($"BARBERO NOT AVAILABLE - Status: {disponibilidad}");
                Mensaje = $"El barbero seleccionado no está disponible en este momento (Estado: {disponibilidad}). Por favor selecciona otro barbero.";
                OnGet();
                return Page();
            }
            // =====================================================

            // We'll need parsedInicio/parsedFin. If HorarioID provided but times are missing, fetch horario and find an available slot inside it.
            TimeSpan parsedInicio = default;
            TimeSpan parsedFin = default;
            HorarioBarbero? horario = null;

            if (HorarioID > 0)
            {
                horario = await _context.HorariosBarbero.FirstOrDefaultAsync(h => h.HorarioID == HorarioID && h.BarberoID == BarberoID && h.Fecha.HasValue && h.Fecha.Value.Date == parsedFecha.Date);
                if (horario == null)
                {
                    Console.WriteLine($"Horario with ID {HorarioID} not found for barbero {BarberoID} on date {parsedFecha.Date}");
                    Mensaje = "Horario no disponible.";
                    OnGet();
                    return Page();
                }

                // If times weren't provided in the form, find the first available slot inside this horario block using the same step logic as GetHorarios
                if (string.IsNullOrEmpty(HoraInicio) || string.IsNullOrEmpty(HoraFin))
                {
                    // load existing reservas for that barbero and date (excluding cancelled ones)
                    var reservas = await _context.Reservas
                        .Where(r => r.BarberoID == BarberoID && r.FechaReserva.Date == parsedFecha.Date && r.Estado != "Cancelada")
                        .ToListAsync();

                    // compute step (gcd of all service durations)
                    int defaultStep = 15;
                    var allDurations = await _context.Servicios.Select(s => s.DuracionMinutos).Where(d => d > 0).ToListAsync();
                    int gcdMinutes = defaultStep;
                    if (allDurations.Any())
                    {
                        gcdMinutes = allDurations.Aggregate((a, b) => Gcd(a, b));
                        if (gcdMinutes <= 0) gcdMinutes = defaultStep;
                    }

                    if (gcdMinutes > servicio.DuracionMinutos)
                    {
                        gcdMinutes = Math.Max(1, servicio.DuracionMinutos);
                    }

                    var step = TimeSpan.FromMinutes(gcdMinutes);

                    bool foundSlot = false;
                    for (var t = horario.HoraInicio; t + expectedDuration <= horario.HoraFin; t = t.Add(step))
                    {
                        var slotStart = t;
                        var slotEnd = t + expectedDuration;
                        var conflict = reservas.Any(r => r.HoraInicio < slotEnd && r.HoraFin > slotStart);
                        if (!conflict)
                        {
                            parsedInicio = slotStart;
                            parsedFin = slotEnd;
                            HoraInicio = parsedInicio.ToString();
                            HoraFin = parsedFin.ToString();
                            foundSlot = true;
                            Console.WriteLine($"Found fallback slot inside horario: {parsedInicio} - {parsedFin}");
                            break;
                        }
                    }

                    if (!foundSlot)
                    {
                        Mensaje = "No hay espacio disponible en ese horario.";
                        OnGet();
                        return Page();
                    }
                }
            }

            // If we still don't have parsedInicio/parsedFin, try to parse them from the submitted values
            if (parsedFin == default && parsedInicio == default)
            {
                if (!TimeSpan.TryParse(HoraInicio, out parsedInicio) || !TimeSpan.TryParse(HoraFin, out parsedFin))
                {
                    Console.WriteLine($"Failed to parse HoraInicio/HoraFin: {HoraInicio} / {HoraFin}");
                    Mensaje = "Formato de hora inválido.";
                    OnGet();
                    return Page();
                }
            }

            if (parsedFin <= parsedInicio)
            {
                Mensaje = "La hora de fin debe ser posterior a la hora de inicio.";
                OnGet();
                return Page();
            }

            // Check if user is authenticated
            var userName = User.Identity?.Name;
            Console.WriteLine($"User Name: {userName ?? "NULL"}");
            
            if (string.IsNullOrEmpty(userName))
            {
                Console.WriteLine("USER NOT AUTHENTICATED - Redirecting to login");
                return RedirectToPage("/Login/Login");
            }

            // Get the usuario (user)
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.NombreCompleto == userName);
            if (usuario == null)
            {
                Console.WriteLine($"USUARIO NOT FOUND in database with name: {userName}");
                Mensaje = "Usuario no encontrado.";
                OnGet();
                return Page();
            }
            Console.WriteLine($"Usuario found: ID={usuario.UsuarioID}, Nombre={usuario.NombreCompleto}");

            // Ensure there is a Cliente record linked to this Usuario
            var clienteEntity = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioID == usuario.UsuarioID);
            if (clienteEntity == null)
            {
                Console.WriteLine("No Cliente record found for this Usuario. Creating one.");
                clienteEntity = new Cliente
                {
                    UsuarioID = usuario.UsuarioID,
                    Direccion = null,
                    FechaNacimiento = null
                };

                _context.Clientes.Add(clienteEntity);
                await _context.SaveChangesAsync(); // ensure ClienteID is generated
                Console.WriteLine($"Cliente record created: ClienteID={clienteEntity.ClienteID}, UsuarioID={clienteEntity.UsuarioID}");
            }

            // If horario was not loaded earlier (HorarioID == 0), try to find a horario block that contains the requested slot
            if (horario == null)
            {
                horario = await _context.HorariosBarbero.FirstOrDefaultAsync(h => h.BarberoID == BarberoID && h.Fecha.HasValue && h.Fecha.Value.Date == parsedFecha.Date && h.HoraInicio <= parsedInicio && h.HoraFin >= parsedFin && h.Disponible);
            }

            if (horario == null)
            {
                Console.WriteLine("HORARIO BLOCK NOT FOUND");
                Mensaje = "Horario no disponible.";
                OnGet();
                return Page();
            }

            // Ensure requested slot fits inside the horario block
            if (parsedInicio < horario.HoraInicio || parsedFin > horario.HoraFin)
            {
                Console.WriteLine($"Requested slot outside horario block: {parsedInicio} - {parsedFin} vs block {horario.HoraInicio} - {horario.HoraFin}");
                Mensaje = "El horario seleccionado no está dentro de la disponibilidad del barbero.";
                OnGet();
                return Page();
            }

            // Validate service duration
            if (parsedFin - parsedInicio < TimeSpan.FromMinutes(1))
            {
                Mensaje = "Duración del turno inválida.";
                OnGet();
                return Page();
            }

            // Check for overlapping reservations for this barbero on the same date (excluding cancelled ones)
            var overlapping = await _context.Reservas
                .AnyAsync(r => r.BarberoID == BarberoID && r.FechaReserva.Date == parsedFecha.Date && r.HoraInicio < parsedFin && r.HoraFin > parsedInicio && r.Estado != "Cancelada");

            if (overlapping)
            {
                Mensaje = "El horario seleccionado ya está reservado.";
                OnGet();
                return Page();
            }

            // Create the reserva using the selected slot times
            var reserva = new Reserva
            {
                ClienteID = clienteEntity.ClienteID,
                BarberoID = BarberoID,
                ServicioID = ServicioID,
                FechaReserva = parsedFecha,
                HoraInicio = parsedInicio,
                HoraFin = parsedFin,
                Estado = "Pendiente",
                FechaCreacion = DateTime.Now
            };

            Console.WriteLine($"RESERVA CREATED - ClienteID: {reserva.ClienteID}, BarberoID: {reserva.BarberoID}, ServicioID: {reserva.ServicioID}");
            Console.WriteLine($"  FechaReserva: {reserva.FechaReserva}, HoraInicio: {reserva.HoraInicio}, HoraFin: {reserva.HoraFin}");

            try
            {
                _context.Reservas.Add(reserva);
                Console.WriteLine("Reserva added to context");

                // Do not mark the whole horario block as unavailable — allow partial slots

                var recordsAffected = await _context.SaveChangesAsync();
                Console.WriteLine($"=== CHANGES SAVED TO DATABASE SUCCESSFULLY ===");
                Console.WriteLine($"Records affected: {recordsAffected}");

                TempData["SuccessMessage"] = "¡Reserva confirmada exitosamente!";
                return RedirectToPage("/Reservar");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR SAVING TO DATABASE ===");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                string errorDetails = ex.Message;
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                    errorDetails += $" | Inner: {ex.InnerException.Message}";

                    if (ex.InnerException.InnerException != null)
                    {
                        Console.WriteLine($"InnerInnerException: {ex.InnerException.InnerException.Message}");
                        errorDetails += $" | Detail: {ex.InnerException.InnerException.Message}";
                    }
                }

                Mensaje = $"Error al guardar la reserva: {errorDetails}";
                OnGet();
                return Page();
            }
        }

        private static int Gcd(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            if (a == 0) return b;
            if (b == 0) return a;
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }
            return a;
        }
    }
}