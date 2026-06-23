namespace SistemaMatriculas.API.Models
{
    public class Matricula
    {
        public int Id { get; set; }
        public int AlumnoId { get; set; }
        public int CursoId { get; set; }
        public int CicloId { get; set; }
        public DateTime FechaMatricula { get; set; } = DateTime.UtcNow;
        public string Estado { get; set; } = "ACTIVA"; //ACTIVA o BAJA

        public Alumno Alumno {  get; set; }
        public Curso Curso { get; set; }
        public Ciclo Ciclo { get; set; }
    }
}
