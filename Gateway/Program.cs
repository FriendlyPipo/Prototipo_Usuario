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
// Configuración de YARP
builder.Services.AddReverseProxy()
       .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy")); 

var app = builder.Build();

// Configuración del pipeline de YARP
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy(); 
});

app.Run();
