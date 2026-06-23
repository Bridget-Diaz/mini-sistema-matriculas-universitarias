using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.API.Data;
using SistemaMatriculas.API.DTOs;
using SistemaMatriculas.API.Models;

namespace SistemaMatriculas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CicloController : Controller
    {
        private readonly AppDbContext _context;

        public CicloController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CicloResponseDto>>> GetCiclos()
        {
            var ciclos = await _context.Ciclo
                .Select(c => new CicloResponseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    Estado = c.Estado,
                    CreditosMaximos = c.CreditosMaximos
                })
                .ToListAsync();

            return Ok(ciclos);
        }

        
        [HttpGet("activo")] // Devuelve el ciclo actualmente activo (el más común de usar)
        public async Task<ActionResult<CicloResponseDto>> GetCicloActivo()
        {
            var ciclo = await _context.Ciclo
                .FirstOrDefaultAsync(c => c.Estado == "ACTIVO");

            if (ciclo == null)
                return NotFound("No hay ningún ciclo activo actualmente.");

            return Ok(new CicloResponseDto
            {
                Id = ciclo.Id,
                Nombre = ciclo.Nombre,
                FechaInicio = ciclo.FechaInicio,
                FechaFin = ciclo.FechaFin,
                Estado = ciclo.Estado,
                CreditosMaximos = ciclo.CreditosMaximos
            });
        }

        [HttpPost]
        public async Task<ActionResult<CicloResponseDto>> CreateCiclo(CicloCreateDto dto)
        {
            var existe = await _context.Ciclo.AnyAsync(c => c.Nombre == dto.Nombre);
            if (existe)
                return Conflict("Ya existe un ciclo con ese nombre.");

            var ciclo = new Ciclo
            {
                Nombre = dto.Nombre,
                FechaInicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc),  // ← AJUSTE
                FechaFin = DateTime.SpecifyKind(dto.FechaFin, DateTimeKind.Utc),        // ← AJUSTE
                CreditosMaximos = dto.CreditosMaximos,
                Estado = "ACTIVO"
            };

            _context.Ciclo.Add(ciclo);
            await _context.SaveChangesAsync();

            var response = new CicloResponseDto
            {
                Id = ciclo.Id,
                Nombre = ciclo.Nombre,
                FechaInicio = ciclo.FechaInicio,
                FechaFin = ciclo.FechaFin,
                Estado = ciclo.Estado,
                CreditosMaximos = ciclo.CreditosMaximos
            };

            return CreatedAtAction(nameof(GetCiclos), response);
        }

        [HttpPut("{id}/cerrar")] // Cierra un ciclo (para que no se puedan crear más matrículas en él)
        public async Task<IActionResult> CerrarCiclo(int id)
        {
            var ciclo = await _context.Ciclo.FindAsync(id);
            if (ciclo == null)
                return NotFound($"No se encontró el ciclo con Id {id}");

            ciclo.Estado = "CERRADO";
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
