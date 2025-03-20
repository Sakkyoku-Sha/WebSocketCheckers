using WebGameServer;
using WebGameServer.GameLogic;

var builder = WebApplication.CreateBuilder(args);

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders(); // Clear any pre-configured logging providers
builder.Logging.AddConsole(); // Add Console logging
builder.Logging.AddDebug(); // Add Debug output
builder.Logging.SetMinimumLevel(LogLevel.Debug); // Set the minimum log level to Debug or Trace for higher verbosity

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy  =>
        {
            policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowCredentials().AllowAnyMethod();
        });
});

//In Memory Management and Game State 
var game = new GameManager(); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Web Socket Stuff. 
app.UseCors(myAllowSpecificOrigins);
app.UseWebSockets();

app.MapPost("/TryMakeMove", async (CheckersMove move) =>
{
    var validMove = game.TryMove((move.FromX, move.FromY), (move.ToX, move.ToY), out var gameState);

    if (validMove)
    {
        var messageToSend = gameState.ToByteArray();
        await WebSocketHandler.SendMessageToAll(messageToSend); 
    }
    
    return Results.Ok();
});

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        // Accept the WebSocket request
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var gameStateBinary = game._gameState.ToByteArray();
        
        await WebSocketHandler.AddSocketAsync(webSocket, gameStateBinary);
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});


app.Run();
record CheckersMove(int FromX, int FromY, int ToX, int ToY);