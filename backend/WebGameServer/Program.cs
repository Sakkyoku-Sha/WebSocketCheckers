using WebGameServer;
using WebGameServer.API;
using WebGameServer.GameLogic;
using WebGameServer.GameStateManagement;
using WebGameServer.GameStateManagement.GameStatePersistence;
using WebGameServer.GameStateManagement.GameStatePersistence.Postgres;
using WebGameServer.GameStateManagement.KeyValueStore;
using WebGameServer.State;
using WebGameServer.WebSocketEncoding;

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

//In Memory Management and Game State 
var postgresConn = new PostgresConnection();
await postgresConn.Connect();  //BLOCK 

IGameInfoPersistence persist = new PostgresGameInfoPersistence(postgresConn);

var keyGameInfoStore = new InMemoryKeyGameInfoStore(); 
var gameStateManager = new GameManager(keyGameInfoStore);
var clientSocketManagement; 

app.MapPost("/TryMakeMove", async (CheckersMoveRequest move) =>
{
    if (gameStateManager.TryApplyMove(move.gameId, move.FromIndex, move.ToIndex, out GameInfo gameInfo))
    {
        var message = GameHistoryUpdate = 
        WebSocketHandler.SendMessageToAll([gameInfo.Player1, gameInfo.Player2], )
    }; 
    
    var validMove = GameLogic.TryApplyMove(gameState, move.FromIndex, move.ToIndex);

    if (validMove)
    {
        var messageToSend = GameMovesByteSerializer.SerializeMostRecentHistory(gameState);
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
        var newUserId = Guid.NewGuid();  
        
        await WebSocketHandler.AddSocketAsync(webSocket, newUserId);
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});

app.MapGraphQL();

app.Run();
record struct CheckersMoveRequest(Guid gameId, byte FromIndex, byte ToIndex);