using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.API.Data;
using SistemaMatriculas.API.DTOs;
using SistemaMatriculas.API.Models;

namespace SistemaMatriculas.API.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AlumnosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AlumnosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet] // Devuelve la lista de todos los alumnos | GET: api/alumnos
        public async Task<ActionResult<IEnumerable<AlumnoResponseDto>>> GetAlumnos()
        {
            var alumnos = await _context.Alumnos
                .Select(a => new AlumnoResponseDto
                {
                    Id = a.Id,
                    Nombres = a.Nombres,
                    Apellidos = a.Apellidos,
                    Correo = a.Correo,
                    FechaRegistro = a.FechaRegistro
                })
                .ToListAsync();
            return Ok(alumnos);
        }

        [HttpGet("{id}")]// Devuelve un alumno por su Id | GET: api/alumnos/5
        public async Task<ActionResult<AlumnoResponseDto>> GetAlumno(int id)
        {
            var alumno = await _context.Alumnos.FindAsync(id);

            if (alumno == null)
                return NotFound($"No se encontró el alumno con Id {id}");

            return Ok(new AlumnoResponseDto
            {
                Id = alumno.Id,
                Nombres = alumno.Nombres,
                Apellidos = alumno.Apellidos,
                Correo = alumno.Correo,
                FechaRegistro = alumno.FechaRegistro
            });
        }

        [HttpPost] // Crea un nuevo alumno | POST: api/alumnos
        public async Task<ActionResult<AlumnoResponseDto>> CreateAlumno(AlumnoCreateDto dto)
        {
            var existe = await _context.Alumnos
                .AnyAsync(a => a.Correo == dto.Correo);

            if (existe)
                return Conflict("Ya existe un alumno con ese correo.");

            // Generar código automático: C + AÑO + secuencial de 5 dígitos
            var año = DateTime.UtcNow.Year;
            var cantidad = await _context.Alumnos
                .CountAsync(a => a.CodigoAlumno.StartsWith($"C{año}"));
            var codigoAlumno = $"C{año}{(cantidad + 1):D5}"; // Ej: C202600001

            var alumno = new Alumno
            {
                CodigoAlumno = codigoAlumno,
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Correo = dto.Correo,
                FechaRegistro = DateTime.UtcNow
            };

            _context.Alumnos.Add(alumno);
            await _context.SaveChangesAsync();

            var response = new AlumnoResponseDto
            {
                Id = alumno.Id,
                CodigoAlumno = alumno.CodigoAlumno,
                Nombres = alumno.Nombres,
                Apellidos = alumno.Apellidos,
                Correo = alumno.Correo,
                FechaRegistro = alumno.FechaRegistro
            };

            return CreatedAtAction(nameof(GetAlumno), new { id = alumno.Id }, response);
        }


        [HttpGet("codigo/{codigo}")] // Buscar alumno por su codigo | GET: api/alumnos/codigo/C202600001
        public async Task<ActionResult<AlumnoResponseDto>> GetAlumnoPorCodigo(string codigo)
        {
            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.CodigoAlumno == codigo);

            if (alumno == null)
                return NotFound($"No se encontró el alumno con código {codigo}");

            return Ok(new AlumnoResponseDto
            {
                Id = alumno.Id,
                CodigoAlumno = alumno.CodigoAlumno,
                Nombres = alumno.Nombres,
                Apellidos = alumno.Apellidos,
                Correo = alumno.Correo,
                FechaRegistro = alumno.FechaRegistro
            });
        }


        [HttpDelete("{id}")] // Elimina un alumno por su Id | DELETE: api/alumnos/5
        public async Task<IActionResult> DeleteAlumno(int id)
        {
            var alumno = await _context.Alumnos.FindAsync(id);

            if (alumno == null)
                return NotFound($"No se encontró el alumno con Id {id}");

            _context.Alumnos.Remove(alumno);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
