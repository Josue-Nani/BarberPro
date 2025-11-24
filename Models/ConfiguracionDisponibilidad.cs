using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarberPro.Models
{
    [Table("ConfiguracionesDisponibilidad")]
    public class ConfiguracionDisponibilidad
    {
        [Key]
        public int ConfiguracionID { get; set; }
        
        [Required]
        public int BarberoID { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha Inicio")]
        public DateTime FechaInicio { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha Fin")]
        public DateTime FechaFin { get; set; }
        
        [Display(Name = "Lunes")]
        public bool LunesLibre { get; set; }
        
        [Display(Name = "Martes")]
        public bool MartesLibre { get; set; }
        
        [Display(Name = "Miércoles")]
        public bool MiercolesLibre { get; set; }
        
        [Display(Name = "Jueves")]
        public bool JuevesLibre { get; set; }
        
        [Display(Name = "Viernes")]
        public bool ViernesLibre { get; set; }
        
        [Display(Name = "Sábado")]
        public bool SabadoLibre { get; set; }
        
        [Display(Name = "Domingo")]
        public bool DomingoLibre { get; set; }
        
        // CRÍTICO: Horario de trabajo usado para generar horarios automáticamente en días no libres
        [Display(Name = "Hora Inicio Trabajo")]
        public TimeSpan? HoraInicioTrabajo { get; set; }
        
        [Display(Name = "Hora Fin Trabajo")]
        public TimeSpan? HoraFinTrabajo { get; set; }
        
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public int AdminCreadorID { get; set; }
        
        [ForeignKey("BarberoID")]
        public Barbero? Barbero { get; set; }
        
        [ForeignKey("AdminCreadorID")]
        public Usuario? AdminCreador { get; set; }
        
        public bool EsDiaLibre(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => LunesLibre,
                DayOfWeek.Tuesday => MartesLibre,
                DayOfWeek.Wednesday => MiercolesLibre,
                DayOfWeek.Thursday => JuevesLibre,
                DayOfWeek.Friday => ViernesLibre,
                DayOfWeek.Saturday => SabadoLibre,
                DayOfWeek.Sunday => DomingoLibre,
                _ => false
            };
        }
        
        [NotMapped]
        public string DiasLibresTexto
        {
            get
            {
                var dias = new List<string>();
                if (LunesLibre) dias.Add("Lun");
                if (MartesLibre) dias.Add("Mar");
                if (MiercolesLibre) dias.Add("Mié");
                if (JuevesLibre) dias.Add("Jue");
                if (ViernesLibre) dias.Add("Vie");
                if (SabadoLibre) dias.Add("Sáb");
                if (DomingoLibre) dias.Add("Dom");
                return dias.Count > 0 ? string.Join(", ", dias) : "Ninguno";
            }
        }
    }
}
