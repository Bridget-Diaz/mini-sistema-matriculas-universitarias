namespace SistemaMatriculas.API.DTOs
{
    public class CicloCreateDto
    {
        public string Nombre { get; set; }            // Ej: "2026-1"
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int CreditosMaximos { get; set; } = 22;
    }

    public class CicloResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; }
        public int CreditosMaximos { get; set; }
    }
}
