using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.Lambda.Models;

namespace SistemaMatriculas.Lambda.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }

        public DbSet<Ciclo> Ciclo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Le decimos que el nombre real de la tabla es "Cursos"
            modelBuilder.Entity<Curso>().ToTable("Cursos");
            modelBuilder.Entity<Alumno>().ToTable("Alumnos");
            modelBuilder.Entity<Matricula>().ToTable("Matriculas");
            modelBuilder.Entity<Ciclo>().ToTable("Ciclo");
        }
    }
}
