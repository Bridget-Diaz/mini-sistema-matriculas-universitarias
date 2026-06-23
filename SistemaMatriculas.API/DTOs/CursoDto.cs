namespace SistemaMatriculas.API.DTOs
{
    public class CursoCreateDto
    {
        public string Nombre { get; set; }
        public int CuposDisponibles { get; set; }
        public int Creditos { get; set; }
    }

    public class CursoResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public int CuposDisponibles { get; set; }
        public int Creditos { get; set; }
    }
}
