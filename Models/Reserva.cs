using System.ComponentModel.DataAnnotations;

namespace BarberPro.Models;

public class Reserva
{
    [Key]
    public int ReservaID { get; set; }

    // Cliente que reserva (FK a Usuarios)
    public int ClienteID { get; set; }

    // Barbero asignado
    public int BarberoID { get; set; }

    // Servicio reservado
    public int ServicioID { get; set; }

    // Fecha de la reserva (solo fecha)
    public DateTime FechaReserva { get; set; }

    // Hora de inicio y fin
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }

    // Estado de la reserva (Pendiente, Confirmada, Cancelada, etc.)
    public string? Estado { get; set; }

    // Fecha de creación del registro
    public DateTime FechaCreacion { get; set; }
}
