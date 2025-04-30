using System.Security.Claims;
using KcAuthentication.Core.Interfaces;
using KcAuthentication.Infrastructure.Repositories;
using KcAuthentication.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

/* Por implementar
builder.Services.KeycloakConfiguration(builder.Configuration);
*/

builder.Services.Configure<ClientUrl>(
    builder.Configuration.GetSection("Url"));

builder.Services.AddScoped<IKeycloakRepository, KeycloakRepository>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello");

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();
