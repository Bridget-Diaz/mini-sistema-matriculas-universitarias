namespace SistemaMatriculas.API.Models
{
    public class Alumno
    {
        public int Id { get; set; }
        public string CodigoAlumno { get; set; } // ← Ej: C202600001
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public ICollection<Matricula> Matriculas { get; set; }
    }
}
