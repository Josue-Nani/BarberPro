using Microsoft.EntityFrameworkCore;
using BarberPro.Data;
using BarberPro.Models;

namespace BarberPro.Services
{
    public class DisponibilidadService
    {
        private readonly BarberContext _context;

        public DisponibilidadService(BarberContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si una fecha específica está configurada como día libre para un barbero
        /// </summary>
        public async Task<bool> EsDiaLibre(int barberoId, DateTime fecha)
        {
            var configuraciones = await _context.ConfiguracionesDisponibilidad
                .Where(c => c.BarberoID == barberoId &&
                           c.FechaInicio.Date <= fecha.Date &&
                           c.FechaFin.Date >= fecha.Date)
                .ToListAsync();

            foreach (var config in configuraciones)
            {
                if (config.EsDiaLibre(fecha.DayOfWeek))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Obtiene todas las fechas configuradas como días libres para un barbero en un rango
        /// </summary>
        public async Task<List<DateTime>> ObtenerDiasLibres(int barberoId, DateTime inicio, DateTime fin)
        {
            var diasLibres = new List<DateTime>();
            
            var configuraciones = await _context.ConfiguracionesDisponibilidad
                .Where(c => c.BarberoID == barberoId &&
                           ((c.FechaInicio <= fin && c.FechaFin >= inicio)))
                .ToListAsync();

            if (!configuraciones.Any())
            {
                return diasLibres;
            }

            // Iterar sobre cada día en el rango
            for (var fecha = inicio.Date; fecha <= fin.Date; fecha = fecha.AddDays(1))
            {
                foreach (var config in configuraciones)
                {
                    // Verificar si la fecha está dentro del rango de esta configuración
                    if (fecha >= config.FechaInicio.Date && fecha <= config.FechaFin.Date)
                    {
                        // Verificar si el día de la semana está marcado como libre
                        if (config.EsDiaLibre(fecha.DayOfWeek))
                        {
                            if (!diasLibres.Contains(fecha))
                            {
                                diasLibres.Add(fecha);
                            }
                            break;
                        }
                    }
                }
            }

            return diasLibres;
        }

        /// <summary>
        /// Valida que una configuración no se solape con otras existentes
        /// </summary>
        public async Task<bool> ValidarConfiguracion(ConfiguracionDisponibilidad config)
        {
            // Validar que fecha inicio <= fecha fin
            if (config.FechaInicio.Date > config.FechaFin.Date)
            {
                return false;
            }

            // Validar que al menos un día de la semana esté seleccionado
            if (!config.LunesLibre && !config.MartesLibre && !config.MiercolesLibre &&
                !config.JuevesLibre && !config.ViernesLibre && !config.SabadoLibre && !config.DomingoLibre)
            {
                return false;
            }

            // Verificar solapamiento con otras configuraciones del mismo barbero
            var solapamiento = await _context.ConfiguracionesDisponibilidad
                .Where(c => c.BarberoID == config.BarberoID &&
                           c.ConfiguracionID != config.ConfiguracionID && // Excluir la misma configuración en caso de edición
                           ((c.FechaInicio <= config.FechaFin && c.FechaFin >= config.FechaInicio)))
                .AnyAsync();

            return !solapamiento;
        }

        /// <summary>
        /// Obtiene todas las configuraciones activas para un barbero en una fecha específica
        /// </summary>
        public async Task<List<ConfiguracionDisponibilidad>> ObtenerConfiguracionesActivas(int barberoId, DateTime fecha)
        {
            return await _context.ConfiguracionesDisponibilidad
                .Where(c => c.BarberoID == barberoId &&
                           c.FechaInicio.Date <= fecha.Date &&
                           c.FechaFin.Date >= fecha.Date)
                .Include(c => c.Barbero)
                    .ThenInclude(b => b!.Usuario)
                .Include(c => c.AdminCreador)
                .ToListAsync();
        }

        /// <summary>
        /// Genera horarios semanales para un barbero en un rango de fechas,
        /// excluyendo días libres configurados y solicitudes aprobadas
        /// </summary>
        public async Task<(int created, int skipped, List<string> warnings)> GenerarHorariosSemanales(
            int barberoId,
            DateTime fechaInicio,
            DateTime fechaFin,
            TimeSpan horaInicio,
            TimeSpan horaFin,
            bool lunesLibre,
            bool martesLibre,
            bool miercolesLibre,
            bool juevesLibre,
            bool viernesLibre,
            bool sabadoLibre,
            bool domingoLibre,
            bool eliminarExistentes = false)
        {
            int horariosCreados = 0;
            int horariosOmitidos = 0;
            var advertencias = new List<string>();

            // Obtener solicitudes aprobadas que afecten este rango
            var solicitudesAprobadas = await _context.SolicitudesDisponibilidad
                .Where(s => s.BarberoID == barberoId &&
                           s.Estado == "Aprobada" &&
                           s.FechaInicio <= fechaFin &&
                           s.FechaFin >= fechaInicio)
                .ToListAsync();

            // Si se solicita, eliminar horarios existentes en el rango
            if (eliminarExistentes)
            {
                var horariosExistentes = await _context.HorariosBarbero
                    .Where(h => h.BarberoID == barberoId &&
                               h.Fecha.HasValue &&
                               h.Fecha.Value.Date >= fechaInicio.Date &&
                               h.Fecha.Value.Date <= fechaFin.Date)
                    .ToListAsync();

                // Verificar si hay reservas en estos horarios
                var hayReservas = await _context.Reservas
                    .AnyAsync(r => r.BarberoID == barberoId &&
                                  r.FechaReserva.Date >= fechaInicio.Date &&
                                  r.FechaReserva.Date <= fechaFin.Date &&
                                  r.Estado != "Cancelada");

                if (hayReservas)
                {
                    advertencias.Add("ADVERTENCIA: Existen reservas en el período. No se eliminaron horarios existentes para proteger las reservas.");
                }
                else if (horariosExistentes.Any())
                {
                    _context.HorariosBarbero.RemoveRange(horariosExistentes);
                    await _context.SaveChangesAsync();
                    advertencias.Add($"Se eliminaron {horariosExistentes.Count} horarios existentes sin reservas.");
                }
            }

            // Generar horarios día por día
            for (var fecha = fechaInicio.Date; fecha <= fechaFin.Date; fecha = fecha.AddDays(1))
            {
                // Verificar si este día de la semana está marcado como libre
                bool esDiaLibreConfig = fecha.DayOfWeek switch
                {
                    DayOfWeek.Monday => lunesLibre,
                    DayOfWeek.Tuesday => martesLibre,
                    DayOfWeek.Wednesday => miercolesLibre,
                    DayOfWeek.Thursday => juevesLibre,
                    DayOfWeek.Friday => viernesLibre,
                    DayOfWeek.Saturday => sabadoLibre,
                    DayOfWeek.Sunday => domingoLibre,
                    _ => false
                };

                if (esDiaLibreConfig)
                {
                    horariosOmitidos++;
                    continue;
                }

                // Verificar si hay solicitud aprobada para esta fecha
                bool tieneSolicitudAprobada = solicitudesAprobadas.Any(s =>
                    s.FechaInicio.Date <= fecha && s.FechaFin.Date >= fecha);

                if (tieneSolicitudAprobada)
                {
                    horariosOmitidos++;
                    advertencias.Add($"Día {fecha:dd/MM/yyyy} omitido por solicitud de disponibilidad aprobada");
                    continue;
                }

                // Verificar si ya existe un horario para esta fecha
                var horarioExistente = await _context.HorariosBarbero
                    .AnyAsync(h => h.BarberoID == barberoId && h.Fecha.HasValue && h.Fecha.Value.Date == fecha);

                if (horarioExistente && !eliminarExistentes)
                {
                    horariosOmitidos++;
                    advertencias.Add($"Ya existe horario para {fecha:dd/MM/yyyy}");
                    continue;
                }

                // Crear el horario
                var nuevoHorario = new HorarioBarbero
                {
                    BarberoID = barberoId,
                    Fecha = fecha,
                    HoraInicio = horaInicio,
                    HoraFin = horaFin,
                    Disponible = true
                };

                _context.HorariosBarbero.Add(nuevoHorario);
                horariosCreados++;
            }

            // Guardar todos los cambios
            await _context.SaveChangesAsync();

            return (horariosCreados, horariosOmitidos, advertencias);
        }
    }
}
