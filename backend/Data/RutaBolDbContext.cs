using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Models;

namespace RutaBol.Api.Data;

public class RutaBolDbContext : DbContext
{
    public RutaBolDbContext(DbContextOptions<RutaBolDbContext> options) : base(options) { }

    // Estas son nuestras tablas
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Micro> Micros => Set<Micro>();
    public DbSet<LineaTransporte> Lineas => Set<LineaTransporte>();
    public DbSet<UbicacionMicro> UbicacionesMicro => Set<UbicacionMicro>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<SolicitudCategoria> SolicitudesCategoria => Set<SolicitudCategoria>();
    public DbSet<Pasaje> Pasajes => Set<Pasaje>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Correo).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.Correo).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Rol).HasMaxLength(30);
        });

        modelBuilder.Entity<LineaTransporte>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Codigo).IsRequired().HasMaxLength(20);
            entity.HasIndex(l => l.Codigo).IsUnique();
            entity.Property(l => l.Activa).HasDefaultValue(true);
        });

        modelBuilder.Entity<Micro>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Linea).IsRequired().HasMaxLength(20);
            entity.Property(m => m.NumeroInterno).IsRequired().HasMaxLength(20);
            entity.Property(m => m.Estado).HasMaxLength(30);
            entity.Property(m => m.Activo).HasDefaultValue(true);
            entity.HasOne<LineaTransporte>().WithMany().HasForeignKey(m => m.LineaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(m => new { m.Linea, m.NumeroInterno }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Micros_Capacidad",
                "\"CapacidadMaxima\" > 0 AND \"PasajerosActuales\" >= 0 AND \"PasajerosActuales\" <= \"CapacidadMaxima\""));
        });

        // GPS de los micros. La relación conserva el historial del micro.
        modelBuilder.Entity<UbicacionMicro>(entity =>
        {
            entity.HasKey(u => u.MicroId);
            entity.HasOne<Micro>().WithOne().HasForeignKey<UbicacionMicro>(u => u.MicroId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(u => u.ConductorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(u => u.ConductorUsuarioId);
        });

        modelBuilder.Entity<Tarifa>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Monto).HasPrecision(10, 2).IsRequired();
            entity.Property(t => t.Moneda).IsRequired().HasMaxLength(20).HasDefaultValue("BOB");
            entity.Property(t => t.Vigente).HasDefaultValue(true);
            entity.Property(t => t.FechaInicio).IsRequired();
            entity.Property(t => t.FechaFin);
            entity.Property(t => t.UsuarioAdministradorId);
            entity.HasIndex(t => new { t.Nombre, t.Vigente });
            entity.ToTable(t => t.HasCheckConstraint("CK_Tarifas_Monto", "\"Monto\" > 0"));
        });

        // Guardamos las solicitudes de categoría con acceso controlado
        modelBuilder.Entity<SolicitudCategoria>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Categoria).IsRequired().HasMaxLength(80);
            entity.Property(s => s.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(s => s.CorreoInstitucional).HasMaxLength(150);
            entity.Property(s => s.NombreArchivo).IsRequired().HasMaxLength(100);
            entity.Property(s => s.TipoArchivo).IsRequired().HasMaxLength(30);
            entity.Property(s => s.Estado).IsRequired().HasMaxLength(20);
            entity.HasIndex(s => new { s.UsuarioId, s.Estado });
            entity.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        // Guardamos el importe vigente al momento de cada simulación
        modelBuilder.Entity<Pasaje>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.CategoriaAplicada).IsRequired().HasMaxLength(80);
            entity.Property(p => p.Monto).HasPrecision(10, 2);
            entity.Property(p => p.CodigoComprobante).IsRequired().HasMaxLength(32);
            entity.Property(p => p.Estado).IsRequired().HasMaxLength(30);
            entity.HasIndex(p => p.CodigoComprobante).IsUnique();
            entity.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Micro>().WithMany().HasForeignKey(p => p.MicroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Tarifa>().WithMany().HasForeignKey(p => p.TarifaId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_Pasajes_Monto", "\"Monto\" > 0"));
        });
    }
}
