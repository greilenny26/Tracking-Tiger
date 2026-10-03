using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IReloj, RelojSistema>();

builder.Services.AddDbContext<ContextoDatos>(opciones =>
    opciones.UseSqlite("Data Source=trackingtiger.db"));

var app = builder.Build();

app.MapControllers();

app.Run();
