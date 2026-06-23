using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.API.Models;

namespace SistemaMatriculas.API.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Ciclo> Ciclo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Alumno>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.CodigoAlumno)
                .IsRequired()
                .HasMaxLength(20);

                entity.HasIndex(a => a.CodigoAlumno)
                .IsUnique();

                entity.Property(a => a.Correo)
                .IsRequired()
                .HasMaxLength(100);

                entity.HasIndex(a => a.Correo)
                .IsUnique();

                entity.Property(a => a.Nombres)
                .IsRequired()
                .HasMaxLength(100);

                entity.Property(a => a.Apellidos)
                .IsRequired()
                .HasMaxLength(100);
            });

            modelBuilder.Entity<Curso>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Codigo)
                .IsRequired()
                .HasMaxLength(20);

                entity.HasIndex(c => c.Codigo)
                .IsUnique();

                entity.Property(c => c.Nombre)
                .IsRequired()
                .HasMaxLength(150);
            });

            modelBuilder.Entity<Matricula>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVA");

                entity.HasOne(m => m.Alumno)
                .WithMany(a => a.Matriculas)
                .HasForeignKey(m => m.AlumnoId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Curso)
                .WithMany(c => c.Matriculas)
                .HasForeignKey(m => m.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Ciclo)
                .WithMany(c => c.Matriculas)
                .HasForeignKey(m => m.CicloId)
                .OnDelete(DeleteBehavior.Restrict); // No se puede borrar un ciclo con matrículas
            });

            modelBuilder.Entity<Ciclo>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Nombre)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.HasIndex(c => c.Nombre)
                      .IsUnique();   // No pueden existir dos ciclos con el mismo nombre

                entity.Property(c => c.Estado)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("ACTIVO");
            });
        }
    }
}
