using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.Lambda.Data;
using SistemaMatriculas.Lambda.Models;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SistemaMatriculas.Lambda
{
    public class Functions

    {   // El nombre exacto del secreto que creaste en Secrets Manager
        private const string SecretName = "sistemamatriculas/db-credentials";
        // Esta clase representa la estructura del secreto que AWS genera automáticamente
        // cuando eliges "Credentials for Amazon RDS database"
        private class DbSecret
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
            public string host { get; set; } = string.Empty;
            public int port { get; set; }
            public string dbname { get; set; } = string.Empty;
        }

        /**************************************************CURSOS*************************************************/
        public class CursoCreateRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public int CuposDisponibles { get; set; }
            public int Creditos { get; set; }
        }

        // Método que se conecta a Secrets Manager y obtiene la contraseña real
        private async Task<string> ObtenerConnectionStringAsync(ILambdaContext context)
        {
            context.Logger.LogLine("Consultando Secrets Manager...");
            // Cliente para hablar con Secrets Manager
            using var client = new AmazonSecretsManagerClient();
            var request = new GetSecretValueRequest { SecretId = SecretName };
            // Pide el secreto a AWS
            var response = await client.GetSecretValueAsync(request);
            // El secreto viene como un JSON en texto, lo convertimos a un objeto C#
            var secret = JsonSerializer.Deserialize<DbSecret>(response.SecretString)!;

            context.Logger.LogLine("Secreto obtenido correctamente.");
            // Armamos el connection string de Npgsql con los datos del secreto
            return $"Host={secret.host};Port={secret.port};Database=SistemaMatriculas;Username={secret.username};Password={secret.password}";
        }

        private async Task<AppDbContext> GetDbContextAsync(ILambdaContext context)
        {
            var connectionString = await ObtenerConnectionStringAsync(context);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new AppDbContext(options);
        }

        // ───────────────────────────────────────────
        // GET /cursos → Lista todos los cursos con cupos
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ListarCursos(ILambdaContext context)
        {
            context.Logger.LogLine("Iniciando consulta de cursos...");
            using var db = await GetDbContextAsync(context);
            var cursos = await db.Cursos.Where(c => c.CuposDisponibles > 0).ToListAsync();
            context.Logger.LogLine($"Se encontraron {cursos.Count} cursos disponibles.");

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(cursos),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }


        // ───────────────────────────────────────────
        // GET /cursos/{id} → Consulta un curso por Id
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ObtenerCurso(APIGatewayProxyRequest request, ILambdaContext context)
        {
            // El id viene como parámetro de ruta, igual que en EliminarCurso
            var id = int.Parse(request.PathParameters["id"]);

            context.Logger.LogLine($"Buscando curso con Id {id}...");

            using var db = await GetDbContextAsync(context);
            var curso = await db.Cursos.FindAsync(id);

            if (curso == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el curso con Id {id}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(curso),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // POST /cursos → Crea un curso con código automático
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> CrearCurso(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Creando nuevo curso...");

            // El body del POST viene como string en request.Body, lo deserializamos
            // PropertyNameCaseInsensitive ignora si las propiedades vienen en minúsculas
            var input = JsonSerializer.Deserialize<CursoCreateRequest>(
                request.Body!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;

            using var db = await GetDbContextAsync(context);

            var prefijo = new string(input.Nombre.ToUpper().Where(char.IsLetter).Take(3).ToArray());
            var cantidad = await db.Cursos.CountAsync(c => c.Codigo.StartsWith(prefijo));
            var codigo = $"{prefijo}{(cantidad + 1):D3}";

            var curso = new Curso
            {
                Nombre = input.Nombre,
                Codigo = codigo,
                CuposDisponibles = input.CuposDisponibles,
                Creditos = input.Creditos
            };

            db.Cursos.Add(curso);
            await db.SaveChangesAsync();
            context.Logger.LogLine($"Curso creado con código {codigo}");

            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Body = JsonSerializer.Serialize(curso),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // DELETE /cursos/{id} → Elimina un curso
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> EliminarCurso(APIGatewayProxyRequest request, ILambdaContext context)
        {
            // El id viene como parámetro de ruta en request.PathParameters
            var id = int.Parse(request.PathParameters["id"]);

            context.Logger.LogLine($"Eliminando curso con Id {id}...");
            using var db = await GetDbContextAsync(context);
            var curso = await db.Cursos.FindAsync(id);

            if (curso == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el curso con Id {id}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            db.Cursos.Remove(curso);
            await db.SaveChangesAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { mensaje = "Curso eliminado correctamente." }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        /******************************************************ALUMNOS*************************************************/

        // ───────────────────────────────────────────
        // GET /alumnos → Lista todos los alumnos
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ListarAlumnos(ILambdaContext context)
        {
            context.Logger.LogLine("Listando alumnos...");
            using var db = await GetDbContextAsync(context);

            var alumnos = await db.Alumnos.ToListAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(alumnos),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // GET /alumnos/{id} → Consulta un alumno por Id
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ObtenerAlumno(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var id = int.Parse(request.PathParameters["id"]);
            context.Logger.LogLine($"Buscando alumno con Id {id}...");

            using var db = await GetDbContextAsync(context);
            var alumno = await db.Alumnos.FindAsync(id);

            if (alumno == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el alumno con Id {id}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(alumno),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // GET /alumnos/codigo/{codigo} → Buscar por código de alumno
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ObtenerAlumnoPorCodigo(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var codigo = request.PathParameters["codigo"];
            context.Logger.LogLine($"Buscando alumno con código {codigo}...");

            using var db = await GetDbContextAsync(context);
            var alumno = await db.Alumnos.FirstOrDefaultAsync(a => a.CodigoAlumno == codigo);

            if (alumno == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el alumno con código {codigo}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(alumno),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // POST /alumnos → Crea un alumno con código automático
        // ───────────────────────────────────────────
        public class AlumnoCreateRequest
        {
            public string Nombres { get; set; } = string.Empty;
            public string Apellidos { get; set; } = string.Empty;
            public string Correo { get; set; } = string.Empty;
        }

        public async Task<APIGatewayProxyResponse> CrearAlumno(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Creando nuevo alumno...");

            var input = JsonSerializer.Deserialize<AlumnoCreateRequest>(
                request.Body!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;

            using var db = await GetDbContextAsync(context);

            var existe = await db.Alumnos.AnyAsync(a => a.Correo == input.Correo);
            if (existe)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 409,
                    Body = JsonSerializer.Serialize(new { mensaje = "Ya existe un alumno con ese correo." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // Mismo algoritmo de generación de código que en tu API
            var año = DateTime.UtcNow.Year;
            var cantidad = await db.Alumnos.CountAsync(a => a.CodigoAlumno.StartsWith($"C{año}"));
            var codigoAlumno = $"C{año}{(cantidad + 1):D5}";

            var alumno = new Alumno
            {
                CodigoAlumno = codigoAlumno,
                Nombres = input.Nombres,
                Apellidos = input.Apellidos,
                Correo = input.Correo,
                FechaRegistro = DateTime.UtcNow
            };

            db.Alumnos.Add(alumno);
            await db.SaveChangesAsync();
            context.Logger.LogLine($"Alumno creado con código {codigoAlumno}");

            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Body = JsonSerializer.Serialize(alumno),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // DELETE /alumnos/{id} → Elimina un alumno
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> EliminarAlumno(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var id = int.Parse(request.PathParameters["id"]);
            context.Logger.LogLine($"Eliminando alumno con Id {id}...");

            using var db = await GetDbContextAsync(context);
            var alumno = await db.Alumnos.FindAsync(id);

            if (alumno == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el alumno con Id {id}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            db.Alumnos.Remove(alumno);
            await db.SaveChangesAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { mensaje = "Alumno eliminado correctamente." }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        /******************************************************MATRICULAS*********************************************/

        // ───────────────────────────────────────────
        // GET /matriculas → Lista todas las matrículas con nombres
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ListarMatriculas(ILambdaContext context)
        {
            context.Logger.LogLine("Listando matrículas...");
            using var db = await GetDbContextAsync(context);

            // Hacemos el JOIN manualmente, ya que no configuramos propiedades de navegación
            var matriculas = await db.Matriculas
                .Join(db.Alumnos, m => m.AlumnoId, a => a.Id, (m, a) => new { m, a })
                .Join(db.Cursos, ma => ma.m.CursoId, c => c.Id, (ma, c) => new
                {
                    ma.m.Id,
                    ma.m.FechaMatricula,
                    ma.m.Estado,
                    NombreAlumno = ma.a.Nombres + " " + ma.a.Apellidos,
                    NombreCurso = c.Nombre
                })
                .ToListAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(matriculas),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // POST /matriculas → Matricula a un alumno en un curso (por código)
        // ───────────────────────────────────────────
        public class MatriculaCreateRequest
        {
            public string CodigoAlumno { get; set; } = string.Empty;
            public string CodigoCurso { get; set; } = string.Empty;
        }

        public async Task<APIGatewayProxyResponse> CrearMatricula(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Creando nueva matrícula...");

            var input = JsonSerializer.Deserialize<MatriculaCreateRequest>(
                request.Body!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;

            using var db = await GetDbContextAsync(context);

            // 1. Buscar el ciclo activo
            var ciclo = await db.Ciclo.FirstOrDefaultAsync(c => c.Estado == "ACTIVO");
            if (ciclo == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { mensaje = "No hay ningún ciclo académico activo en este momento." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // 2. Buscar alumno por código
            var alumno = await db.Alumnos.FirstOrDefaultAsync(a => a.CodigoAlumno == input.CodigoAlumno);
            if (alumno == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el alumno con código {input.CodigoAlumno}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // 3. Buscar curso por código
            var curso = await db.Cursos.FirstOrDefaultAsync(c => c.Codigo == input.CodigoCurso);
            if (curso == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró el curso con código {input.CodigoCurso}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // 4. Verificar cupos
            if (curso.CuposDisponibles <= 0)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { mensaje = "El curso no tiene cupos disponibles." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // 5. Verificar que no esté ya matriculado en este curso, en este ciclo
            var yaMatriculado = await db.Matriculas.AnyAsync(m =>
                m.AlumnoId == alumno.Id && m.CursoId == curso.Id &&
                m.CicloId == ciclo.Id && m.Estado == "ACTIVA");

            if (yaMatriculado)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 409,
                    Body = JsonSerializer.Serialize(new { mensaje = "El alumno ya está matriculado en este curso, en este ciclo." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // 6. Verificar créditos máximos del ciclo
            var creditosActuales = await db.Matriculas
                .Where(m => m.AlumnoId == alumno.Id && m.CicloId == ciclo.Id && m.Estado == "ACTIVA")
                .Join(db.Cursos, m => m.CursoId, c => c.Id, (m, c) => c.Creditos)
                .SumAsync();

            if (creditosActuales + curso.Creditos > ciclo.CreditosMaximos)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new
                    {
                        mensaje = $"No se puede matricular: excede el máximo de {ciclo.CreditosMaximos} créditos del ciclo {ciclo.Nombre}. " +
                                  $"Créditos actuales: {creditosActuales}, créditos del curso: {curso.Creditos}."
                    }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            // 7. Crear la matrícula
            var matricula = new Matricula
            {
                AlumnoId = alumno.Id,
                CursoId = curso.Id,
                CicloId = ciclo.Id,
                FechaMatricula = DateTime.UtcNow,
                Estado = "ACTIVA"
            };

            curso.CuposDisponibles--;

            db.Matriculas.Add(matricula);
            await db.SaveChangesAsync();

            context.Logger.LogLine($"Matrícula creada: {alumno.CodigoAlumno} → {curso.Codigo} en ciclo {ciclo.Nombre}");

            var response = new
            {
                matricula.Id,
                matricula.FechaMatricula,
                matricula.Estado,
                NombreAlumno = alumno.Nombres + " " + alumno.Apellidos,
                NombreCurso = curso.Nombre,
                Ciclo = ciclo.Nombre
            };

            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // DELETE /matriculas/{id} → Da de baja una matrícula (baja lógica)
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> BajaMatricula(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var id = int.Parse(request.PathParameters["id"]);
            context.Logger.LogLine($"Dando de baja matrícula con Id {id}...");

            using var db = await GetDbContextAsync(context);
            var matricula = await db.Matriculas.FindAsync(id);

            if (matricula == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = $"No se encontró la matrícula con Id {id}" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            if (matricula.Estado == "BAJA")
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { mensaje = "La matrícula ya fue dada de baja." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            matricula.Estado = "BAJA";

            // Devolver el cupo al curso
            var curso = await db.Cursos.FindAsync(matricula.CursoId);
            if (curso != null)
                curso.CuposDisponibles++;

            await db.SaveChangesAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { mensaje = "Matrícula dada de baja correctamente." }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        /******************************************************CICLO*************************************************/

        // ───────────────────────────────────────────
        // GET /ciclos → Lista todos los ciclos
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ListarCiclos(ILambdaContext context)
        {
            context.Logger.LogLine("Listando ciclos...");
            using var db = await GetDbContextAsync(context);

            var ciclos = await db.Ciclo.ToListAsync();

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(ciclos),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // GET /ciclos/activo → Devuelve el ciclo activo actual
        // ───────────────────────────────────────────
        public async Task<APIGatewayProxyResponse> ObtenerCicloActivo(ILambdaContext context)
        {
            context.Logger.LogLine("Buscando ciclo activo...");
            using var db = await GetDbContextAsync(context);

            var ciclo = await db.Ciclo.FirstOrDefaultAsync(c => c.Estado == "ACTIVO");

            if (ciclo == null)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new { mensaje = "No hay ningún ciclo activo actualmente." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(ciclo),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // ───────────────────────────────────────────
        // POST /ciclos → Crea un nuevo ciclo
        // ───────────────────────────────────────────
        public class CicloCreateRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
            public int CreditosMaximos { get; set; } = 22;
        }

        public async Task<APIGatewayProxyResponse> CrearCiclo(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Creando nuevo ciclo...");

            var input = JsonSerializer.Deserialize<CicloCreateRequest>(
                request.Body!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            )!;

            using var db = await GetDbContextAsync(context);

            var existe = await db.Ciclo.AnyAsync(c => c.Nombre == input.Nombre);
            if (existe)
                return new APIGatewayProxyResponse
                {
                    StatusCode = 409,
                    Body = JsonSerializer.Serialize(new { mensaje = "Ya existe un ciclo con ese nombre." }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };

            var ciclo = new Ciclo
            {
                Nombre = input.Nombre,
                // Igual que en tu API: forzamos Kind=Utc para que PostgreSQL lo acepte
                FechaInicio = DateTime.SpecifyKind(input.FechaInicio, DateTimeKind.Utc),
                FechaFin = DateTime.SpecifyKind(input.FechaFin, DateTimeKind.Utc),
                CreditosMaximos = input.CreditosMaximos,
                Estado = "ACTIVO"
            };

            db.Ciclo.Add(ciclo);
            await db.SaveChangesAsync();
            context.Logger.LogLine($"Ciclo creado: {ciclo.Nombre}");

            return new APIGatewayProxyResponse
            {
                StatusCode = 201,
                Body = JsonSerializer.Serialize(ciclo),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}