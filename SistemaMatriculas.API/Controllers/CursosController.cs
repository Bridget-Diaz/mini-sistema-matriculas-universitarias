using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.API.Data;
using SistemaMatriculas.API.DTOs;
using SistemaMatriculas.API.Models;

namespace SistemaMatriculas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CursosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CursosController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet] // GET: api/cursos
        public async Task<ActionResult<IEnumerable<CursoResponseDto>>> GetCursos()
        {
            var cursos = await _context.Cursos
                .Select(c => new CursoResponseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Codigo = c.Codigo,
                    CuposDisponibles = c.CuposDisponibles,
                    Creditos = c.Creditos
                })
                .ToListAsync();

            return Ok(cursos);
        }

        
        [HttpGet("{id}")] // GET: api/cursos/5
        public async Task<ActionResult<CursoResponseDto>> GetCurso(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);

            if (curso == null)
                return NotFound($"No se encontró el curso con Id {id}");

            return Ok(new CursoResponseDto
            {
                Id = curso.Id,
                Nombre = curso.Nombre,
                Codigo = curso.Codigo,
                CuposDisponibles = curso.CuposDisponibles,
                Creditos = curso.Creditos
            });
        }

        
        [HttpPost] // POST: api/cursos
        public async Task<ActionResult<CursoResponseDto>> CreateCurso(CursoCreateDto dto)
        {
            // Generamos el código automáticamente
            // Toma las 3 primeras letras del nombre en mayúsculas + número secuencial
            // Ejemplo: "Programación I" → "PRO001"
            var prefijo = new string(dto.Nombre
                .ToUpper()
                .Where(char.IsLetter)  // Solo letras, sin espacios ni caracteres especiales
                .Take(3)               // Las primeras 3 letras
                .ToArray());

            // Cuenta cuántos cursos ya existen con ese prefijo para el número secuencial
            var cantidad = await _context.Cursos
                .CountAsync(c => c.Codigo.StartsWith(prefijo));

            var codigo = $"{prefijo}{(cantidad + 1):D3}";

            var existe = await _context.Cursos
                .AnyAsync(c => c.Codigo == codigo);

            if (existe)
                return Conflict("Ya existe un curso con ese código.");

            var curso = new Curso
            {
                Nombre = dto.Nombre,
                Codigo = codigo,        // Asignamos el código generado
                CuposDisponibles = dto.CuposDisponibles,
                Creditos = dto.Creditos
            };

            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            var response = new CursoResponseDto
            {
                Id = curso.Id,
                Nombre = curso.Nombre,
                Codigo = curso.Codigo,
                CuposDisponibles = curso.CuposDisponibles,
                Creditos = curso.Creditos
            };

            return CreatedAtAction(nameof(GetCurso), new { id = curso.Id }, response);
        }



        [HttpDelete("{id}")] // DELETE: api/cursos/5
        public async Task<IActionResult> DeleteCurso(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);

            if (curso == null)
                return NotFound($"No se encontró el curso con Id {id}");

            _context.Cursos.Remove(curso);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
