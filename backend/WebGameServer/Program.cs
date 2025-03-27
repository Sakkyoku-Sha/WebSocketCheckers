using WebGameServer;
using WebGameServer.API;
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
            policy.WithOrigins("http://localhost:5000").AllowAnyHeader().AllowCredentials().AllowAnyMethod();
        });
});

//Setup GraphQL 
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

//In Memory Management and Game State 
var gameState = new GameState(); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
    app.MapGraphQLWebSocket();
}

//Web Socket Stuff. 
app.UseCors(myAllowSpecificOrigins);
app.UseWebSockets();

app.MapPost("/TryMakeMove", async (CheckersMove move) =>
{
    var validMove = GameLogic.TryApplyMove(ref gameState, move);

    if (validMove)
    {
        var messageToSend = GameStateByteSerializer.SerializeMostRecentHistory(gameState);
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
        var entireMoveHistoryBytes = GameStateByteSerializer.SerializeEntireHistory(gameState);
        
        await WebSocketHandler.AddSocketAsync(webSocket, entireMoveHistoryBytes);
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});

app.MapGraphQL();

app.Run();