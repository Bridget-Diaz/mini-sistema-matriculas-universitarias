
namespace SistemaMatriculas.Lambda.Models
{
    public class Ciclo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public int CreditosMaximos { get; set; } = 22;
    }
}
