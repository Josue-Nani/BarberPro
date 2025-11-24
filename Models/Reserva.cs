using System.ComponentModel.DataAnnotations;

namespace BarberPro.Models
{
    public class Reserva
    {
        [Key]
        public int ReservaID { get; set; }

        public int ClienteID { get; set; }
        public int BarberoID { get; set; }
        public int ServicioID { get; set; }

        public DateTime FechaReserva { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }

        public string? Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        public Cliente? Cliente { get; set; }
        public Barbero? Barbero { get; set; }
        public Servicio? Servicio { get; set; }
    }
}
