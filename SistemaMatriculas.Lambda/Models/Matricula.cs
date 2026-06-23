using System;
using System.Collections.Generic;

namespace SistemaMatriculas.Lambda.Models
{
    public class Matricula
    {
        public int Id { get; set; }
        public int AlumnoId { get; set; }
        public int CursoId { get; set; }
        public int CicloId { get; set; }
        public DateTime FechaMatricula { get; set; } = DateTime.UtcNow;
        public string Estado { get; set; } = "ACTIVA";
    }
}
