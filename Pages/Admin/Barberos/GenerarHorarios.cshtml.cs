using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using BarberPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin.Barberos
{
    [Authorize(Policy = "AdminOnly")]
    public class GenerarHorariosModel : PageModel
    {
        private readonly BarberContext _context;

        public GenerarHorariosModel(BarberContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<BarberoModel> Barberos { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Debes seleccionar un barbero")]
            [Display(Name = "Barbero")]
            public int BarberoID { get; set; }

            [Required(ErrorMessage = "La fecha de inicio es requerida")]
            [DataType(DataType.Date)]
            [Display(Name = "Fecha Inicio")]
            public DateTime FechaInicio { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "La fecha de fin es requerida")]
            [DataType(DataType.Date)]
            [Display(Name = "Fecha Fin")]
            public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(1);

            [Required(ErrorMessage = "La hora de inicio es requerida")]
            [DataType(DataType.Time)]
            [Display(Name = "Hora Inicio Trabajo")]
            public TimeSpan HoraInicio { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM

            [Required(ErrorMessage = "La hora de fin es requerida")]
            [DataType(DataType.Time)]
            [Display(Name = "Hora Fin Trabajo")]
            public TimeSpan HoraFin { get; set; } = new TimeSpan(18, 0, 0); // 6:00 PM

            [Display(Name = "Lunes Libre")]
            public bool LunesLibre { get; set; }

            [Display(Name = "Martes Libre")]
            public bool MartesLibre { get; set; }

            [Display(Name = "Miércoles Libre")]
            public bool MiercolesLibre { get; set; }

            [Display(Name = "Jueves Libre")]
            public bool JuevesLibre { get; set; }

            [Display(Name = "Viernes Libre")]
            public bool ViernesLibre { get; set; }

            [Display(Name = "Sábado Libre")]
            public bool SabadoLibre { get; set; } = true; // Default: Sábado libre

            [Display(Name = "Domingo Libre")]
            public bool DomingoLibre { get; set; } = true; // Default: Domingo libre

            [Display(Name = "Eliminar horarios existentes")]
            public bool EliminarExistentes { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadBarberos();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadBarberos();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validar fechas
            if (Input.FechaInicio.Date > Input.FechaFin.Date)
            {
                ModelState.AddModelError("Input.FechaFin", "La fecha fin debe ser igual o posterior a la fecha inicio");
                return Page();
            }

            // Validar horas
            if (Input.HoraInicio >= Input.HoraFin)
            {
                ModelState.AddModelError("Input.HoraFin", "La hora fin debe ser posterior a la hora inicio");
                return Page();
            }

            // Validar que al menos un día sea laborable
            if (Input.LunesLibre && Input.MartesLibre && Input.MiercolesLibre &&
                Input.JuevesLibre && Input.ViernesLibre && Input.SabadoLibre && Input.DomingoLibre)
            {
                ModelState.AddModelError("", "Debes tener al menos un día laborable. No pueden ser todos días libres.");
                return Page();
            }

            try
            {
                // Verificar si ya existe un horario que se solape con este período
                var horariosExistentes = await _context.HorariosBarbero
                    .Where(h => h.BarberoID == Input.BarberoID &&
                               ((h.Fecha <= Input.FechaFin && (h.FechaFin ?? h.Fecha) >= Input.FechaInicio)))
                    .ToListAsync();

                if (horariosExistentes.Any() && !Input.EliminarExistentes)
                {
                    var mensaje = $"Ya existen {horariosExistentes.Count} configuración(es) de horario que se solapan con este período. ";
                    mensaje += "Activa la opción 'Eliminar horarios existentes' si deseas reemplazarlos.";
                    ModelState.AddModelError("", mensaje);
                    return Page();
                }

                // Si se solicita eliminar, verificar que no haya reservas
                if (Input.EliminarExistentes && horariosExistentes.Any())
                {
                    var hayReservas = await _context.Reservas
                        .AnyAsync(r => r.BarberoID == Input.BarberoID &&
                                      r.FechaReserva.Date >= Input.FechaInicio.Date &&
                                      r.FechaReserva.Date <= Input.FechaFin.Date &&
                                      r.Estado != "Cancelada");

                    if (hayReservas)
                    {
                        ModelState.AddModelError("", "No se pueden eliminar horarios existentes porque hay reservas activas en el período.");
                        return Page();
                    }

                    // Eliminar horarios existentes
                    _context.HorariosBarbero.RemoveRange(horariosExistentes);
                    await _context.SaveChangesAsync();
                }

                // Crear el nuevo horario de período
                var nuevoHorario = new HorarioBarbero
                {
                    BarberoID = Input.BarberoID,
                    Fecha = Input.FechaInicio,
                    FechaFin = Input.FechaFin,
                    HoraInicio = Input.HoraInicio,
                    HoraFin = Input.HoraFin,
                    Disponible = true,
                    LunesLibre = Input.LunesLibre,
                    MartesLibre = Input.MartesLibre,
                    MiercolesLibre = Input.MiercolesLibre,
                    JuevesLibre = Input.JuevesLibre,
                    ViernesLibre = Input.ViernesLibre,
                    SabadoLibre = Input.SabadoLibre,
                    DomingoLibre = Input.DomingoLibre
                };

                _context.HorariosBarbero.Add(nuevoHorario);
                await _context.SaveChangesAsync();

                // ConfiguracionesDisponibilidad table removed - only using HorariosBarbero now

                TempData["SuccessMessage"] = $"✅ Horario de período creado exitosamente para {Input.FechaInicio:dd/MM/yyyy} - {Input.FechaFin:dd/MM/yyyy}";
                return RedirectToPage("/Admin/HorariosBarbero/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al generar horarios: {ex.Message}");
                return Page();
            }
        }

        private async Task LoadBarberos()
        {
            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => b.Usuario != null)
                .OrderBy(b => b.Usuario!.NombreCompleto)
                .ToListAsync();
        }
    }
}
