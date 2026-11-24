
using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Models;

namespace RutaBol.Api.Data;

public class RutaBolDbContext : DbContext
{
    public RutaBolDbContext(
        DbContextOptions<RutaBolDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Micro> Micros => Set<Micro>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.NombreCompleto)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(u => u.Correo)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(u => u.Correo)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Rol)
                .HasMaxLength(30);
        });

        modelBuilder.Entity<Micro>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Linea)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(m => m.NumeroInterno)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(m => m.Estado)
                .HasMaxLength(30);

            entity.HasIndex(m => new
            {
                m.Linea,
                m.NumeroInterno
            }).IsUnique();

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Micros_Capacidad",
                    "\"CapacidadMaxima\" > 0 AND " +
                    "\"PasajerosActuales\" >= 0 AND " +
                    "\"PasajerosActuales\" <= \"CapacidadMaxima\""
                )
            );
        });
    }
}
