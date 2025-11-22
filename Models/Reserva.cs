using System.ComponentModel.DataAnnotations;

namespace BarberPro.Models
{
    public class Reserva
    {
        [Key]
        public int ReservaID { get; set; }

        // Foreign keys
        public int ClienteID { get; set; }

        public int BarberoID { get; set; }

        public int ServicioID { get; set; }

        // Fecha y hora de la reserva
        public DateTime FechaReserva { get; set; }

        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }

        // Estado de la reserva
        public string? Estado { get; set; }

        // Fecha de creación del registro
        public DateTime FechaCreacion { get; set; }

        // Navigation properties
        public Cliente? Cliente { get; set; }
        public Barbero? Barbero { get; set; }
        public Servicio? Servicio { get; set; }
    }
}
