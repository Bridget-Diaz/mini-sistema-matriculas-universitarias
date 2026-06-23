using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.API.Data;
using SistemaMatriculas.API.DTOs;
using SistemaMatriculas.API.Models;

namespace SistemaMatriculas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatriculasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MatriculasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet] // GET: api/matriculas
        public async Task<ActionResult<IEnumerable<MatriculaResponseDto>>> GetMatriculas()
        {
            var matriculas = await _context.Matriculas
                .Include(m => m.Alumno)  // JOIN con tabla Alumno
                .Include(m => m.Curso)   // JOIN con tabla Curso
                .Select(m => new MatriculaResponseDto
                {
                    Id = m.Id,
                    FechaMatricula = m.FechaMatricula,
                    Estado = m.Estado,
                    NombreAlumno = m.Alumno.Nombres + " " + m.Alumno.Apellidos,
                    NombreCurso = m.Curso.Nombre
                })
                .ToListAsync();

            return Ok(matriculas);
        }


        [HttpPost] // POST: api/matriculas
        public async Task<ActionResult<MatriculaResponseDto>> CreateMatricula(MatriculaCreateDto dto)
        {
            // 1. Buscar el ciclo activo
            var ciclo = await _context.Ciclo.FirstOrDefaultAsync(c => c.Estado == "ACTIVO");
            if (ciclo == null)
                return BadRequest("No hay ningún ciclo académico activo en este momento.");

            // 2. Buscar alumno por código
            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.CodigoAlumno == dto.CodigoAlumno);
            if (alumno == null)
                return NotFound("No se encontró el alumno con ese código.");

            // 3. Buscar curso por código
            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.Codigo == dto.CodigoCurso);
            if (curso == null)
                return NotFound("No se encontró el curso con ese código.");

            // 4. Verificar cupos del curso
            if (curso.CuposDisponibles <= 0)
                return BadRequest("El curso no tiene cupos disponibles.");

            // 5. Verificar que no esté ya matriculado en este curso, en este ciclo
            var yaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.AlumnoId == alumno.Id &&
                               m.CursoId == curso.Id &&
                               m.CicloId == ciclo.Id &&
                               m.Estado == "ACTIVA");

            if (yaMatriculado)
                return Conflict("El alumno ya está matriculado en este curso, en este ciclo.");

            // 6. NUEVO: Verificar créditos máximos del ciclo
            var creditosActuales = await _context.Matriculas
                .Where(m => m.AlumnoId == alumno.Id &&
                            m.CicloId == ciclo.Id &&
                            m.Estado == "ACTIVA")
                .Join(_context.Cursos, m => m.CursoId, c => c.Id, (m, c) => c.Creditos)
                .SumAsync();

            if (creditosActuales + curso.Creditos > ciclo.CreditosMaximos)
                return BadRequest(
                    $"No se puede matricular: excede el máximo de {ciclo.CreditosMaximos} créditos del ciclo {ciclo.Nombre}. " +
                    $"Créditos actuales: {creditosActuales}, créditos del curso: {curso.Creditos}.");

            // 7. Crear la matrícula
            var matricula = new Matricula
            {
                AlumnoId = alumno.Id,
                CursoId = curso.Id,
                CicloId = ciclo.Id,   // ← se asigna automáticamente el ciclo activo
                FechaMatricula = DateTime.UtcNow,
                Estado = "ACTIVA"
            };

            curso.CuposDisponibles--;

            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            return Ok(new MatriculaResponseDto
            {
                Id = matricula.Id,
                FechaMatricula = matricula.FechaMatricula,
                Estado = matricula.Estado,
                NombreAlumno = alumno.Nombres + " " + alumno.Apellidos,
                NombreCurso = curso.Nombre
            });
        }

        [HttpDelete("{id}")] // Da de baja una matrícula (no la elimina, cambia estado a BAJA) | DELETE: api/matriculas/5
        public async Task<IActionResult> BajaMatricula(int id)
        {
            var matricula = await _context.Matriculas
                .Include(m => m.Curso)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matricula == null)
                return NotFound($"No se encontró la matrícula con Id {id}");

            if (matricula.Estado == "BAJA")
                return BadRequest("La matrícula ya fue dada de baja.");

            // Cambiar estado en vez de eliminar → buena práctica en sistemas académicos
            matricula.Estado = "BAJA";

            // Devolver el cupo al curso
            matricula.Curso.CuposDisponibles++;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
