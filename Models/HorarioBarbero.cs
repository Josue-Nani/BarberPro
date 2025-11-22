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

        public DateTime Fecha { get; set; }

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        public bool Disponible { get; set; } = true;

        // Navigation to Barbero
        [ForeignKey("BarberoID")]
        public Barbero? Barbero { get; set; }

        // Computed property for dropdown display (not mapped to DB)
        [NotMapped]
        public string DisplayText => $"{Fecha:dd/MM/yyyy} - {HoraInicio:hh\\:mm} a {HoraFin:hh\\:mm}";
    }
}
