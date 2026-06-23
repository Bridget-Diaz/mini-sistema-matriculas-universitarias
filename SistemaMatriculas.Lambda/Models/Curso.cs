
namespace SistemaMatriculas.Lambda.Models
{
    public class Curso
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public int CuposDisponibles { get; set; }
        public int Creditos { get; set; }
    }
}
