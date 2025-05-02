using Microsoft.EntityFrameworkCore;
using Users.Core.Repositories;
using Users.Infrastructure.Database;
using Users.Infrastructure.Repositories;
using Users.Application.Handlers;
using MediatR;
using Users.Infrastructure.Interfaces;
using Users.Core.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSwagger", builder =>
    {
        builder.AllowAnyOrigin() // Para pruebas locales
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnectionUser")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
/* builder.Services.AddScoped<IAuthService>(); */

builder.Services.AddMediatR(typeof(CreateUserCommandHandler).Assembly);
builder.Services.AddMediatR(typeof(GetUserByIdQueryHandler).Assembly);


builder.Services.AddTransient<IUserDbContext, UserDbContext>();
//YARP

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Prueba Api");
    });
    app.UseCors("AllowSwagger");
}

app.UseHttpsRedirection();

app.UseRouting(); 
app.MapControllers();
app.MapGet("/hello", () => "Hello World!");

app.Run();