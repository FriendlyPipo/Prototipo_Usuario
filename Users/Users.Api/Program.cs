using Microsoft.EntityFrameworkCore;
using Users.Domain.Interfaces;
using Users.Infrastructure.Data;
using Users.Infrastructure.Repositories;
using Users.Application.Handlers;
using MediatR;
using Users.Infrastructure.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();



builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnectionUser")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
/* builder.Services.AddScoped<IAuthService>(); */

builder.Services.AddMediatR(typeof(CreateUserCommandHandler).Assembly);
builder.Services.AddMediatR(typeof(GetUserByIdQueryHandler).Assembly);

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
}

app.UseHttpsRedirection();
app.UseRouting(); 
app.MapControllers();
app.MapGet("/hello", () => "Hello World!");

app.Run();