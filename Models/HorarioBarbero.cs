using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models
{
    [Table("HorariosBarbero")]
    public class HorarioBarbero
    {
        [Key]
        public int HorarioID { get; set; }

        public int BarberoID { get; set; }

        [Display(Name = "Fecha Inicio")]
        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        public DateTime? Fecha { get; set; }
        
        [Display(Name = "Fecha Fin")]
        public DateTime? FechaFin { get; set; }

        [Display(Name = "Hora Inicio")]
        public TimeSpan HoraInicio { get; set; }

        [Display(Name = "Hora Fin")]
        public TimeSpan HoraFin { get; set; }

        public bool Disponible { get; set; } = true;
        
        // Días de la semana libres (para horarios de período)
        public bool LunesLibre { get; set; }
        public bool MartesLibre { get; set; }
        public bool MiercolesLibre { get; set; }
        public bool JuevesLibre { get; set; }
        public bool ViernesLibre { get; set; }
        public bool SabadoLibre { get; set; }
        public bool DomingoLibre { get; set; }

        // Navigation to Barbero
        [ForeignKey("BarberoID")]
        public Barbero? Barbero { get; set; }

        // Helper method to check if a specific day of week is free
        public bool EsDiaLibre(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => LunesLibre,
                DayOfWeek.Tuesday => MartesLibre,
                DayOfWeek.Wednesday => MiercolesLibre,
                DayOfWeek.Thursday => JuevesLibre,
                DayOfWeek.Friday => ViernesLibre,
                DayOfWeek.Saturday => SabadoLibre,
                DayOfWeek.Sunday => DomingoLibre,
                _ => false
            };
        }

        // Computed property for dropdown display (not mapped to DB)
        [NotMapped]
        public string DisplayText => FechaFin.HasValue 
            ? $"{Fecha?.ToString("dd/MM/yyyy") ?? "N/A"} - {FechaFin:dd/MM/yyyy} | {HoraInicio:hh\\:mm} a {HoraFin:hh\\:mm}"
            : $"{Fecha?.ToString("dd/MM/yyyy") ?? "N/A"} - {HoraInicio:hh\\:mm} a {HoraFin:hh\\:mm}";
    }
}
