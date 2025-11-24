using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models;

[Table("Clientes")]
public class Cliente
{
    [Key]
    public int ClienteID { get; set; }
    
    public int UsuarioID { get; set; }
    
    [StringLength(250)]
    public string? Direccion { get; set; }
    
    public DateTime? FechaNacimiento { get; set; }
    
    [ForeignKey("UsuarioID")]
    public Usuario? Usuario { get; set; }
}
