using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int UsuarioID { get; set; }

    [Required]
    [StringLength(150)]
    public string NombreCompleto { get; set; }

    [Required]
    [StringLength(150)]
    public string Correo { get; set; }

    [Required]
    [StringLength(300)]
    public string ContrasenaHash { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [StringLength(300)]
    public string? FotoPerfil { get; set; }

    public int RolID { get; set; }

    [ForeignKey("RolID")]
    public Rol? Rol { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public bool? Estado { get; set; }
}
