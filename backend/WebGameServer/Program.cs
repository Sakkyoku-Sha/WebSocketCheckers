using WebGameServer;
using WebGameServer.GameStateManagement.GameStateStore;

// //Setup Server Connections and Internal Data Management. 
// var postgresConn = new PostgresConnection();
// await postgresConn.Connect();  //BLOCK 
//
// IGameInfoPersistence persist = new PostgresGameInfoPersistence(postgresConn);

//Build up memory for game states. 
LocalGameSpace.Initialize();

//Setup Web Server 
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Logging.ClearProviders(); // Clear any pre-configured logging providers
builder.Logging.AddConsole(); // Add Console logging
builder.Logging.AddDebug(); // Add Debug output
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Set the minimum log level to Debug or Trace for higher verbosity

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

app.Run();