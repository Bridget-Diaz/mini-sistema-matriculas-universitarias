namespace SistemaMatriculas.API.DTOs
{
    public class AlumnoCreateDto
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
    }

    public class AlumnoResponseDto
    {
        public int Id { get; set; }
        public string CodigoAlumno { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
