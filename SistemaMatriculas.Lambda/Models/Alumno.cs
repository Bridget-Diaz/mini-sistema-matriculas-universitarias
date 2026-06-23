
namespace SistemaMatriculas.Lambda.Models
{
    public class Alumno
    {
        public int Id { get; set; }
        public string CodigoAlumno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}
