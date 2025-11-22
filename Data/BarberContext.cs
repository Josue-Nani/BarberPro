using Microsoft.EntityFrameworkCore;
using BarberPro.Models;

namespace BarberPro.Data;

public class BarberContext : DbContext
{
    public BarberContext(DbContextOptions<BarberContext> options) : base(options)
    {
    }

    // Ejemplo de DbSet para reservas
    public DbSet<Reserva> Reservas { get; set; }

    // DbSet para usuarios (tabla dbo.Usuarios)
    public DbSet<Usuario> Usuarios { get; set; }

    // DbSet para roles
    public DbSet<Rol> Roles { get; set; }

    // DbSet para clientes
    public DbSet<Cliente> Clientes { get; set; }

    // Nuevos DbSet para funcionalidades de reservas
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Barbero> Barberos { get; set; }
    public DbSet<HorarioBarbero> HorariosBarbero { get; set; }

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

            // configure relationship with Usuario
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
            entity.Property(e => e.HoraInicio).HasColumnName("HoraInicio").HasColumnType("time");
            entity.Property(e => e.HoraFin).HasColumnName("HoraFin").HasColumnType("time");
            entity.Property(e => e.Disponible).HasColumnName("Disponible").HasDefaultValue(true);
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

        // Cliente entity configuration
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(e => e.ClienteID);
            entity.Property(e => e.ClienteID).HasColumnName("ClienteID");
            entity.Property(e => e.UsuarioID).HasColumnName("UsuarioID");
            entity.Property(e => e.Direccion).HasColumnName("Direccion").HasMaxLength(250);
            entity.Property(e => e.FechaNacimiento).HasColumnName("FechaNacimiento").HasColumnType("date");

            // relationship with Usuario
            entity.HasOne(c => c.Usuario)
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioID)
                  .HasConstraintName("FK_Clientes_Usuarios");
        });
    }
}
