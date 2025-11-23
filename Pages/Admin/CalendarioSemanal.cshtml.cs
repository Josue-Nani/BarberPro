using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;
using BarberoModel = BarberPro.Models.Barbero;

namespace BarberPro.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class CalendarioSemanalModel : PageModel
    {
        private readonly BarberContext _context;
        private readonly Services.DisponibilidadService _disponibilidadService;

        public CalendarioSemanalModel(BarberContext context, Services.DisponibilidadService disponibilidadService)
        {
            _context = context;
            _disponibilidadService = disponibilidadService;
        }

        public DateTime InicioSemana { get; set; }
        public DateTime FinSemana { get; set; }
        public List<DiaCalendario> Dias { get; set; } = new List<DiaCalendario>();
        public List<BarberoModel> Barberos { get; set; } = new List<BarberoModel>();
        public List<int> Horas { get; set; } = new List<int>();

        public async Task OnGetAsync(DateTime? fecha)
        {
            // Si no se especifica fecha, usar la semana actual
            var fechaBase = fecha ?? DateTime.Now;
            
            // Calcular inicio de semana (lunes)
            int diasDesdeInicio = ((int)fechaBase.DayOfWeek - 1 + 7) % 7;
            InicioSemana = fechaBase.Date.AddDays(-diasDesdeInicio);
            FinSemana = InicioSemana.AddDays(6);

            // Obtener todos los barberos
            Barberos = await _context.Barberos
                .Include(b => b.Usuario)
                .Where(b => b.Usuario != null)
                .ToListAsync();

            // Generar lista de horas (8:00 AM - 8:00 PM)
            Horas = Enumerable.Range(8, 13).ToList(); // 8 to 20 (8 PM)

            // Crear días de la semana
            Dias = new List<DiaCalendario>();
            for (int i = 0; i < 7; i++)
            {
                var dia = InicioSemana.AddDays(i);

                // Define day boundaries to compare ranges
                var dayStart = dia.Date;
                var dayEnd = dayStart.AddDays(1);

                // Load horarios that either have Fecha == this day OR are a period (Fecha..FechaFin) that contains this day
                var horariosDia = await _context.HorariosBarbero
                    .Include(h => h.Barbero)
                    .ThenInclude(b => b!.Usuario)
                    .Where(h => (
                        h.BarberoID > 0 && h.Fecha.HasValue && (
                        (h.Fecha.Value.Date == dayStart) || (h.FechaFin.HasValue && h.Fecha.Value.Date <= dayStart && h.FechaFin.Value.Date >= dayStart)
                    )))
                    .ToListAsync();

                // Exclude horarios that explicitly mark this weekday as free within a period
                var dow = dia.DayOfWeek;
                horariosDia = horariosDia.Where(h => !h.EsDiaLibre(dow)).ToList();

                // Cargar reservas del día (todas excepto canceladas)
                var reservasDia = await _context.Reservas
                    .Include(r => r.Cliente)
                    .ThenInclude(c => c!.Usuario)
                    .Include(r => r.Servicio)
                    .Include(r => r.Barbero)
                    .Where(r => r.FechaReserva.Date == dia.Date && r.Estado != "Cancelada")
                    .ToListAsync();

                // Determine which barberos are configured as day off for this date
                var barberosLibres = new HashSet<int>();
                foreach (var b in Barberos)
                {
                    try
                    {
                        var esLibre = await _disponibilidadService.EsDiaLibre(b.BarberoID, dia);
                        if (esLibre) barberosLibres.Add(b.BarberoID);
                    }
                    catch
                    {
                        // ignore errors per-barbero to avoid breaking calendar render
                    }
                }

                Console.WriteLine($"Fecha {dia.Date:yyyy-MM-dd}: {reservasDia.Count} reservas encontradas");
                foreach (var res in reservasDia)
                {
                    Console.WriteLine($"  - Reserva #{res.ReservaID}: Barbero {res.BarberoID}, {res.HoraInicio}-{res.HoraFin}, Estado: {res.Estado}");
                }

                Dias.Add(new DiaCalendario
                {
                    Fecha = dia,
                    NombreDia = dia.ToString("dddd", new System.Globalization.CultureInfo("es-ES")),
                    Horarios = horariosDia,
                    Reservas = reservasDia,
                    BarberosLibres = barberosLibres
                });
            }
        }

        // Métodos helper para la vista
        public HorarioBarbero? ObtenerHorario(DateTime dia, int hora, int barberoId)
        {
            var diaCalendario = Dias.FirstOrDefault(d => d.Fecha.Date == dia.Date);
            if (diaCalendario == null) return null;

            // Buscar horario que incluya esta hora (no solo que empiece en esta hora)
            var horaActual = new TimeSpan(hora, 0, 0);
            var horaSiguiente = new TimeSpan(hora + 1, 0, 0);
            
            return diaCalendario.Horarios.FirstOrDefault(h =>
                h.BarberoID == barberoId &&
                h.HoraInicio < horaSiguiente && // El horario empieza antes de que termine esta hora
                h.HoraFin > horaActual); // El horario termina después de que empiece esta hora
        }

        public string ObtenerClaseEstado(HorarioBarbero? horario)
        {
            if (horario == null)
                return "bg-slate-800 border-slate-700"; // Sin horario
            
            if (horario.Disponible)
                return "bg-green-900 bg-opacity-30 border-green-600"; // Disponible
            
            return "bg-red-900 bg-opacity-30 border-red-600"; // Ocupado
        }

        public string ObtenerTextoEstado(HorarioBarbero? horario)
        {
            if (horario == null) return "";
            return horario.Disponible ? "Disponible" : "Ocupado";
        }

        public Reserva? ObtenerReserva(DateTime dia, int hora, int barberoId)
        {
            var diaCalendario = Dias.FirstOrDefault(d => d.Fecha.Date == dia.Date);
            if (diaCalendario == null) return null;

            // Buscar reserva que incluya esta hora
            var horaActual = new TimeSpan(hora, 0, 0);
            var horaSiguiente = new TimeSpan(hora + 1, 0, 0);
            
            return diaCalendario.Reservas.FirstOrDefault(r =>
                r.BarberoID == barberoId &&
                r.HoraInicio < horaSiguiente &&
                r.HoraFin > horaActual);
        }
    }

    public class DiaCalendario
    {
        public DateTime Fecha { get; set; }
        public string NombreDia { get; set; } = string.Empty;
        public List<HorarioBarbero> Horarios { get; set; } = new List<HorarioBarbero>();
        public List<Reserva> Reservas { get; set; } = new List<Reserva>();

        // Barbero IDs that are configured as day off for this date
        public HashSet<int> BarberosLibres { get; set; } = new HashSet<int>();
    }
}
