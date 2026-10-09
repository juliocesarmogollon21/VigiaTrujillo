using Microsoft.EntityFrameworkCore;
using VigiaTrujillo.Models;

namespace VigiaTrujillo.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<Evidencia> Evidencias => Set<Evidencia>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ObservacionIncidencia> ObservacionesIncidencia => Set<ObservacionIncidencia>();
    public DbSet<ObraArchivo> ObraArchivos => Set<ObraArchivo>();
    public DbSet<SolicitudInformacion> SolicitudesInformacion => Set<SolicitudInformacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Obra>(e => 
        {
            e.HasIndex(o => o.Cui).IsUnique();
            e.Property(o => o.AvanceFisico).HasPrecision(18, 2);
            e.Property(o => o.Presupuesto).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Incidencia>(e =>
        {
            e.HasIndex(i => i.CodigoSeguimiento).IsUnique();
            e.HasOne(i => i.Obra).WithMany(o => o.Incidencias).HasForeignKey(i => i.ObraId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Evidencia>(e =>
        {
            e.HasOne(x => x.Incidencia).WithMany(i => i.Evidencias)
                .HasForeignKey(x => x.IncidenciaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ObservacionIncidencia>(e =>
        {
            e.ToTable("ObservacionesIncidencia");
            e.HasOne(o => o.Incidencia).WithMany(i => i.Observaciones)
                .HasForeignKey(o => o.IncidenciaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SolicitudInformacion>(e =>
        {
            e.ToTable("SolicitudesInformacion");
            e.HasOne(s => s.Incidencia).WithMany(i => i.Solicitudes)
                .HasForeignKey(s => s.IncidenciaId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.Estado);
        });

        modelBuilder.Entity<ObraArchivo>(e =>
        {
            e.HasOne(a => a.Obra).WithMany(o => o.Archivos)
                .HasForeignKey(a => a.ObraId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(e => e.HasIndex(u => u.NombreUsuario).IsUnique());
    }
}