using Microsoft.EntityFrameworkCore;
using Users.Core.Repositories;
using Users.Infrastructure.Database;
using Users.Infrastructure.Repositories;
using Users.Infrastructure.EventBus.Events;
using Users.Infrastructure.EventBus;
using Users.Infrastructure.EventBus.Consumers;
using Users.Infrastructure.Settings;
using Users.Infrastructure.Exceptions;
using Users.Application.Handlers.Commands;
using Users.Application.Handlers.Queries;
using MediatR;
using Users.Core.Database;
using Users.Core.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Users;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddHttpClient();
builder.Services.AddSwaggerConfiguration();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = "public-client";
        options.Authority = "http://localhost:8180/realms/Artened";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8180/realms/Artened",
            ValidateAudience = true,
            ValidAudience = "public-client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                logger.LogError(context.Exception, "Error en autenticación JWT");
                context.Response.StatusCode = 401;
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

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnectionUser")));

builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<RabbitMQSetting>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    try
    {
        var client = new MongoClient(builder.Configuration["MongoDbSettings:ConnectionString"]);
        var logger = sp.GetRequiredService<ILogger<MongoClient>>();
        logger.LogInformation("Conexión exitosa a MongoDB");
        return client;
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<MongoClient>>();
        logger.LogError(ex, "Error al conectar con MongoDB");
        throw new KeycloakException("Error al conectar con MongoDB", ex);
    }
});

builder.Services.AddSingleton<Task<IConnection>>(async sp =>
{
    var rabbitSettings = sp.GetRequiredService<IOptions<RabbitMQSetting>>().Value;
    var factory = new ConnectionFactory
    {
        HostName = rabbitSettings.HostName,
        UserName = rabbitSettings.UserName,
        Password = rabbitSettings.Password
    };
    
    try
    {
        var connection = await factory.CreateConnectionAsync();
        var logger = sp.GetRequiredService<ILogger<ConnectionFactory>>();
        logger.LogInformation("Conexión exitosa a RabbitMQ");
        return connection;
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<ConnectionFactory>>();
        logger.LogError(ex, "Error al conectar con RabbitMQ");
        throw new KeycloakException("Error al conectar con RabbitMQ", ex);
    }
});

builder.Services.AddSingleton<IConnection>(sp => sp.GetRequiredService<Task<IConnection>>().Result);


builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddScoped<IUserWriteRepository, UserWriteRepository>();
builder.Services.AddScoped<IKeycloakRepository, KeycloakRepository>();
builder.Services.AddSingleton<IRabbitMQChannelFactory, RabbitMQChannelFactory>();
builder.Services.AddSingleton<IEventBus, RabbitMQPublisher>();
builder.Services.AddSingleton<CreateUserConsumer>();
builder.Services.AddSingleton<DeletedUserConsumer>();
builder.Services.AddSingleton<UpdatedUserConsumer>();

builder.Services.AddMediatR(typeof(CreateUserCommandHandler).Assembly);
builder.Services.AddMediatR(typeof(DeleteUserCommandHandler).Assembly);
builder.Services.AddMediatR(typeof(UpdateUserCommandHandler).Assembly);
builder.Services.AddMediatR(typeof(ForgotPasswordCommandHandler).Assembly);
builder.Services.AddMediatR(typeof(GetUserByIdQueryHandler).Assembly);
builder.Services.AddMediatR(typeof(GetAllUsersQueryHandler).Assembly);

builder.Services.AddTransient<IUserDbContext, UserDbContext>();
builder.Services.AddTransient<MongoDbContext>();

var app = builder.Build();


var rabbitMqConnection = app.Services.GetRequiredService<IConnection>();
var createUserConsumer = app.Services.GetRequiredService<CreateUserConsumer>();
var deletedUserConsumer = app.Services.GetRequiredService<DeletedUserConsumer>();
var updatedUserConsumer = app.Services.GetRequiredService<UpdatedUserConsumer>();
await createUserConsumer.Start(rabbitMqConnection);
await deletedUserConsumer.Start(rabbitMqConnection);
await updatedUserConsumer.Start(rabbitMqConnection);

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

app.Run();