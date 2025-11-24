using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models
{
    [Table("SolicitudesDisponibilidad")]
    public class SolicitudDisponibilidad
    {
        [Key]
        public int SolicitudID { get; set; }
        
        public int BarberoID { get; set; }
        
        [Required]
        public DateTime FechaInicio { get; set; }
        
        [Required]
        public DateTime FechaFin { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Motivo { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";
        
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        
        public DateTime? FechaRespuesta { get; set; }
        
        public int? AdminRespondenteID { get; set; }
        
        [StringLength(500)]
        public string? MotivoRechazo { get; set; }
        
        [ForeignKey("BarberoID")]
        public Barbero? Barbero { get; set; }
        
        [ForeignKey("AdminRespondenteID")]
        public Usuario? AdminRespondente { get; set; }
    }
}
