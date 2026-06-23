using Microsoft.EntityFrameworkCore;
using SistemaMatriculas.API.Data;

var builder = WebApplication.CreateBuilder(args);

/*SERVICIOS*/
// Lee la cadena de conexión del appsettings.json
// y registra AppDbContext para que pueda inyectarse en toda la app
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Habilita los controladores (los endpoints HTTP)
builder.Services.AddControllers();

// Habilita Swagger para probar la API desde el navegador
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


/*PIPELINE*/
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Disponible en: https://localhost:{puerto}/swagger
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers(); // Activa el enrutamiento hacia tus controladores

app.Run();
