using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models
{
    [Table("Servicios")]
    public class Servicio
    {
        [Key]
        public int ServicioID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(300)]
        public string Descripcion { get; set; }

        public int DuracionMinutos { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        public bool Estado { get; set; } = true;
    }
}