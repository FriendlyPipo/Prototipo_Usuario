using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(); // Habilita el logging en la consola
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Logging.AddFilter("Yarp", LogLevel.Debug);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // o usa .AllowAnyOrigin() si es solo desarrollo
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// Configuración de CORS

builder.Services.AddHttpClient();
builder.Services.AddControllers();

// Configuración de YARP
builder.Services.AddReverseProxy()
       .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:8180/realms/Artened";
        options.RequireHttpsMetadata = false; // solo en desarrollo
        options.Audience = "public-client";
    });

builder.Services.AddAuthorization();

builder.WebHost.UseUrls("http://localhost:5055");

var app = builder.Build();

// Configuración del pipeline de YARP
app.UseRouting();

// Configuración de CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Mapea los controladores
    endpoints.MapReverseProxy(); // No requiere auth en esta etapa
});



app.Run();
