var builder = WebApplication.CreateBuilder(args);

// Подставляем значения из переменных окружения
builder.Configuration["ReverseProxy:Clusters:authCluster:Destinations:auth:Address"] =
    Environment.GetEnvironmentVariable("AUTH_API_URL");

builder.Configuration["ReverseProxy:Clusters:workoutCluster:Destinations:workout:Address"] =
    Environment.GetEnvironmentVariable("WORKOUT_API_URL");

builder.Configuration["ReverseProxy:Clusters:notificationsCluster:Destinations:notifications:Address"] =
    Environment.GetEnvironmentVariable("NOTIFICATION_API_URL");

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.MapReverseProxy();
app.Run();
