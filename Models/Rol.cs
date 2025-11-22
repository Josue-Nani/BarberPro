using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models;

[Table("Roles")]
public class Rol
{
    [Key]
    public int RolID { get; set; }

    [Required]
    [StringLength(50)]
    public string NombreRol { get; set; }
}
