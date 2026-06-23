namespace SistemaMatriculas.API.DTOs
{
    public class MatriculaCreateDto
    {
        public string CodigoAlumno { get; set; }
        public string CodigoCurso { get; set; }
    }

    public class MatriculaResponseDto
    {
        public int Id { get; set; }
        public DateTime FechaMatricula {  get; set; }
        public string Estado { get; set; }

        public string NombreAlumno {get; set; }
        public string NombreCurso { get; set; }
    }
}
