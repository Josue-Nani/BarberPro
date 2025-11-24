using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models
{
    [Table("Barberos")]
    public class Barbero
    {
        [Key]
        public int BarberoID { get; set; }

        public int UsuarioID { get; set; }

        [StringLength(300)]
        public string? Especialidades { get; set; }

        [StringLength(200)]
        public string? Disponibilidad { get; set; }

        [ForeignKey("UsuarioID")]
        public Usuario? Usuario { get; set; }

        public ICollection<HorarioBarbero>? Horarios { get; set; }
    }
}