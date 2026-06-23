namespace SistemaMatriculas.API.Models
{
    public class Ciclo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }           // Ej: "2026-1"
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = "ACTIVO"; // ACTIVO o CERRADO
        public int CreditosMaximos { get; set; } = 22; // límite típico por ciclo

        // Un ciclo puede tener muchas matrículas
        public ICollection<Matricula> Matriculas { get; set; }
    }
}
