using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Serialization;
var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load("../.env");  

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

app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.EndsWith("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase) == true)
    {
        context.Request.Headers.Remove("Accept-Encoding");
        var originalBody = context.Response.Body;
        await using var mem = new MemoryStream();
        context.Response.Body = mem;

        await next();

        mem.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(mem).ReadToEndAsync();

        try
        {
            var json = JsonNode.Parse(body) ?? new System.Text.Json.Nodes.JsonObject();

            var pathSegs = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var prefix = pathSegs.Length > 0 ? "/" + pathSegs[0] : "";

            var scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? context.Request.Scheme;
            var host = context.Request.Host.Value;
            var serverUrl = $"{scheme}://{host}{prefix}";

            json["servers"] = new JsonArray(
                new JsonObject { ["url"] = serverUrl });

            var newBody = json.ToJsonString();
            var bytes = Encoding.UTF8.GetBytes(newBody);

            context.Response.Body = originalBody;
            context.Response.ContentLength = bytes.Length;
            await context.Response.Body.WriteAsync(bytes);
        }
        catch
        {
            mem.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBody;
            await mem.CopyToAsync(context.Response.Body);
        }

        return;
    }

    await next();
});

app.MapReverseProxy();
app.Run();