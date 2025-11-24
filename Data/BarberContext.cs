using Microsoft.EntityFrameworkCore;
using BarberPro.Models;

namespace BarberPro.Data;

public class BarberContext : DbContext
{
    public BarberContext(DbContextOptions<BarberContext> options) : base(options)
    {
    }

    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Barbero> Barberos { get; set; }
    public DbSet<HorarioBarbero> HorariosBarbero { get; set; }
    public DbSet<SolicitudDisponibilidad> SolicitudesDisponibilidad { get; set; }
    public DbSet<ConfiguracionDisponibilidad> ConfiguracionesDisponibilidad { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicios");
            entity.HasKey(e => e.ServicioID);
            entity.Property(e => e.ServicioID).HasColumnName("ServicioID");
            entity.Property(e => e.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnName("Descripcion").HasMaxLength(300);
            entity.Property(e => e.DuracionMinutos).HasColumnName("DuracionMinutos");
            entity.Property(e => e.Precio).HasColumnName("Precio").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Estado).HasColumnName("Estado").HasDefaultValue(true);
        });

        modelBuilder.Entity<Barbero>(entity =>
        {
            entity.ToTable("Barberos");
            entity.HasKey(e => e.BarberoID);
            entity.Property(e => e.BarberoID).HasColumnName("BarberoID");
            entity.Property(e => e.UsuarioID).HasColumnName("UsuarioID");
            entity.Property(e => e.Especialidades).HasColumnName("Especialidades").HasMaxLength(300);
            entity.Property(e => e.Disponibilidad).HasColumnName("Disponibilidad").HasMaxLength(200);

            // CRÍTICO: Relación entre Barbero y Usuario (un barbero debe tener un usuario)
            entity.HasOne(b => b.Usuario)
                  .WithMany()
                  .HasForeignKey(b => b.UsuarioID)
                  .HasConstraintName("FK_Barberos_Usuarios");
        });

        modelBuilder.Entity<HorarioBarbero>(entity =>
        {
            entity.ToTable("HorariosBarbero");
            entity.HasKey(e => e.HorarioID);
            entity.Property(e => e.HorarioID).HasColumnName("HorarioID");
            entity.Property(e => e.BarberoID).HasColumnName("BarberoID");
            entity.Property(e => e.Fecha).HasColumnName("Fecha").HasColumnType("date");
            entity.Property(e => e.FechaFin).HasColumnName("FechaFin").HasColumnType("date");
            entity.Property(e => e.HoraInicio).HasColumnName("HoraInicio").HasColumnType("time");
            entity.Property(e => e.HoraFin).HasColumnName("HoraFin").HasColumnType("time");
            entity.Property(e => e.Disponible).HasColumnName("Disponible").HasDefaultValue(true);
            entity.Property(e => e.LunesLibre).HasColumnName("LunesLibre");
            entity.Property(e => e.MartesLibre).HasColumnName("MartesLibre");
            entity.Property(e => e.MiercolesLibre).HasColumnName("MiercolesLibre");
            entity.Property(e => e.JuevesLibre).HasColumnName("JuevesLibre");
            entity.Property(e => e.ViernesLibre).HasColumnName("ViernesLibre");
            entity.Property(e => e.SabadoLibre).HasColumnName("SabadoLibre");
            entity.Property(e => e.DomingoLibre).HasColumnName("DomingoLibre");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reservas");
            entity.HasKey(e => e.ReservaID);
            entity.Property(e => e.ReservaID).HasColumnName("ReservaID");
            entity.Property(e => e.ClienteID).HasColumnName("ClienteID");
            entity.Property(e => e.BarberoID).HasColumnName("BarberoID");
            entity.Property(e => e.ServicioID).HasColumnName("ServicioID");
            entity.Property(e => e.FechaReserva).HasColumnName("FechaReserva").HasColumnType("date");
            entity.Property(e => e.HoraInicio).HasColumnName("HoraInicio").HasColumnType("time");
            entity.Property(e => e.HoraFin).HasColumnName("HoraFin").HasColumnType("time");
            entity.Property(e => e.Estado).HasColumnName("Estado");
            entity.Property(e => e.FechaCreacion).HasColumnName("FechaCreacion");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(e => e.ClienteID);
            entity.Property(e => e.ClienteID).HasColumnName("ClienteID");
            entity.Property(e => e.UsuarioID).HasColumnName("UsuarioID");
            entity.Property(e => e.Direccion).HasColumnName("Direccion").HasMaxLength(250);
            entity.Property(e => e.FechaNacimiento).HasColumnName("FechaNacimiento").HasColumnType("date");

            // CRÍTICO: Relación entre Cliente y Usuario (un cliente debe tener un usuario)
            entity.HasOne(c => c.Usuario)
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioID)
                  .HasConstraintName("FK_Clientes_Usuarios");
        });

        modelBuilder.Entity<SolicitudDisponibilidad>(entity =>
        {
            entity.ToTable("SolicitudesDisponibilidad");
            entity.HasKey(e => e.SolicitudID);
            entity.Property(e => e.SolicitudID).HasColumnName("SolicitudID");
            entity.Property(e => e.BarberoID).HasColumnName("BarberoID");
            entity.Property(e => e.FechaInicio).HasColumnName("FechaInicio").HasColumnType("date");
            entity.Property(e => e.FechaFin).HasColumnName("FechaFin").HasColumnType("date");
            entity.Property(e => e.Motivo).HasColumnName("Motivo").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Estado).HasColumnName("Estado").HasMaxLength(50).HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaSolicitud).HasColumnName("FechaSolicitud").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.FechaRespuesta).HasColumnName("FechaRespuesta");
            entity.Property(e => e.AdminRespondenteID).HasColumnName("AdminRespondenteID");
            entity.Property(e => e.MotivoRechazo).HasColumnName("MotivoRechazo").HasMaxLength(500);

            entity.HasOne(s => s.Barbero)
                  .WithMany()
                  .HasForeignKey(s => s.BarberoID)
                  .HasConstraintName("FK_Solicitudes_Barberos");

            // CRÍTICO: DeleteBehavior.Restrict evita eliminación en cascada del admin
            entity.HasOne(s => s.AdminRespondente)
                  .WithMany()
                  .HasForeignKey(s => s.AdminRespondenteID)
                  .HasConstraintName("FK_Solicitudes_Usuarios")
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConfiguracionDisponibilidad>(entity =>
        {
            entity.ToTable("ConfiguracionesDisponibilidad");
            entity.HasKey(e => e.ConfiguracionID);
            entity.Property(e => e.ConfiguracionID).HasColumnName("ConfiguracionID");
            entity.Property(e => e.BarberoID).HasColumnName("BarberoID");
            entity.Property(e => e.FechaInicio).HasColumnName("FechaInicio").HasColumnType("date");
            entity.Property(e => e.FechaFin).HasColumnName("FechaFin").HasColumnType("date");
            entity.Property(e => e.LunesLibre).HasColumnName("LunesLibre").HasDefaultValue(false);
            entity.Property(e => e.MartesLibre).HasColumnName("MartesLibre").HasDefaultValue(false);
            entity.Property(e => e.MiercolesLibre).HasColumnName("MiercolesLibre").HasDefaultValue(false);
            entity.Property(e => e.JuevesLibre).HasColumnName("JuevesLibre").HasDefaultValue(false);
            entity.Property(e => e.ViernesLibre).HasColumnName("ViernesLibre").HasDefaultValue(false);
            entity.Property(e => e.SabadoLibre).HasColumnName("SabadoLibre").HasDefaultValue(false);
            entity.Property(e => e.DomingoLibre).HasColumnName("DomingoLibre").HasDefaultValue(false);
            entity.Property(e => e.HoraInicioTrabajo).HasColumnName("HoraInicioTrabajo").HasColumnType("time");
            entity.Property(e => e.HoraFinTrabajo).HasColumnName("HoraFinTrabajo").HasColumnType("time");
            entity.Property(e => e.FechaCreacion).HasColumnName("FechaCreacion").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.AdminCreadorID).HasColumnName("AdminCreadorID");

            entity.HasOne(c => c.Barbero)
                  .WithMany()
                  .HasForeignKey(c => c.BarberoID)
                  .HasConstraintName("FK_ConfiguracionDisponibilidad_Barberos");

            // CRÍTICO: DeleteBehavior.Restrict evita eliminación en cascada del admin
            entity.HasOne(c => c.AdminCreador)
                  .WithMany()
                  .HasForeignKey(c => c.AdminCreadorID)
                  .HasConstraintName("FK_ConfiguracionDisponibilidad_Usuarios")
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
