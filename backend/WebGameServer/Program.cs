using Microsoft.AspNetCore.Diagnostics;
using WebGameServer;
using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.GameStateManagement.Timers;

// //Setup Server Connections and Internal Data Management. 
// var postgresConn = new PostgresConnection();
// await postgresConn.Connect();  //BLOCK 
//
// IGameInfoPersistence persist = new PostgresGameInfoPersistence(postgresConn);

//Build up memory for game states. 
LocalGameSpace.Initialize();
GameTimers.InitializeTimers();

//Setup Web Server 
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Trace); // Set the minimum log level to Debug or Trace for higher verbosity

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy  =>
        {
            policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowCredentials().AllowAnyMethod();
            policy.WithOrigins("http://localhost:5000").AllowAnyHeader().AllowCredentials().AllowAnyMethod();
        });
});

var app = builder.Build();
app.UseCors(myAllowSpecificOrigins);

app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await SessionSocketHandler.AddSocketAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});

TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(args.Exception, "An unobserved task exception occurred.");
    args.SetObserved(); // Prevents the process from terminating
    Console.WriteLine(args.Exception);
};

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature?.Error is Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred.");
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An error occurred.");
    });
});

var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application Shutting Down...");
    GameTimers.StopTimers();
});

app.Run();