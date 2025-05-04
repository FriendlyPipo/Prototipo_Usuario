using Microsoft.EntityFrameworkCore;
using Users.Core.Repositories;
using Users.Infrastructure.Database;
using Users.Infrastructure.Repositories;
using Users.Application.Handlers;
using MediatR;
using Users.Infrastructure.Interfaces;
using Users.Core.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient();


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Prueba Auth del JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});





builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer( options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = "public-client";
        options.Authority = "http://localhost:8080/realms/Artened";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/Artened",
            ValidateAudience = true,
            ValidAudience = "public-client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
            },
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
            {
                var resourceAccess = context.Principal.FindFirst("resource_access")?.Value;
                if (!string.IsNullOrEmpty(resourceAccess))
                {
                    var resourceAccessJson = System.Text.Json.JsonDocument.Parse(resourceAccess);
                    if (resourceAccessJson.RootElement.TryGetProperty("public-client", out var publiClientElement) &&
                        publiClientElement.TryGetProperty("roles", out var rolesElement))
                    {
                        foreach (var role in rolesElement.EnumerateArray())
                        {
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()));
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
        };    
    });



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
builder.Services.AddScoped<IKeycloakRepository, KeycloakRepository>();

//builder.Services.AddScoped<IAuthService>(); 

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
app.UseAuthentication(); 
app.UseAuthorization(); 
app.MapControllers();
app.MapGet("/hello", () => "Hello World!");

app.Run();