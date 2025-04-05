using NUnit.Framework;
using WebGameServer;
using WebGameServer.API;
using WebGameServer.GameStateManagement;
using WebGameServer.GameStateManagement.GameStatePersistence;
using WebGameServer.GameStateManagement.GameStatePersistence.Postgres;
using WebGameServer.GameStateManagement.KeyValueStore;
using WebGameServer.WebSocketEncoding;
using WebGameServer.WebSocketEncoding.ToClientMessages;

//Setup Server Connections and Internal Data Management. 
var postgresConn = new PostgresConnection();
await postgresConn.Connect();  //BLOCK 

IGameInfoPersistence persist = new PostgresGameInfoPersistence(postgresConn);
var keyGameInfoStore = new InMemoryKeyGameInfoStore(); 
var gameStateManager = new GameStateManager(keyGameInfoStore);
var sessionSocketHandler = new SessionSocketHandler(gameStateManager);

//Setup Web Server 
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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


builder.Services
    .AddSingleton(gameStateManager)
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<UInt64Type>()
    .BindRuntimeType<ulong, UInt64Type>();

var app = builder.Build();
app.UseCors(myAllowSpecificOrigins);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
    app.MapGraphQLWebSocket(); // /graphql endpoint 
}

app.MapPost("/CreateGame", (CreateGameRequest request) =>
{
    Console.WriteLine($"Trying to create game for {request.UserId}");
    if(!sessionSocketHandler.HasClient(request.UserId))
    {
        return Results.BadRequest("No active User with the provided Id");
    }
    if (gameStateManager.PlayerInGame(request.UserId))
    {   
        return Results.Conflict("User already in a game");
    }
    
    var gameInfo = gameStateManager.CreateNewGame(request.UserId);
    var message = new CreateGameResponse(gameInfo.GameId); //For now just let anyone create a game anytime.
    
    Console.WriteLine($"Created game {gameInfo.GameId} for {request.UserId}");
    
    return Results.Json(message, statusCode: 201);
});

app.MapPost("/JoinGame", (JoinGameRequest request) =>
{
    Console.WriteLine($"Trying put {request.UserId} into {request.GameId}");
    if (!sessionSocketHandler.HasClient(request.UserId))
    {
        return Results.BadRequest("No active User with the provided Id");
    }
    if(!gameStateManager.TryGetGameByGameId(request.GameId, out _))
    {
        return Results.BadRequest("No active game with the provided Id");
    }
    if (!gameStateManager.TryJoinGame(request.GameId, request.UserId, out var gameInfo) || gameInfo == null)
    {
       return Results.Conflict("Failed to Join Game");
    }
    
    Console.WriteLine($"Put {request.UserId} into {request.GameId}");
    //Send the player joined message to all players in the game.
    if (gameInfo.Player2 != null)
    {
        var message = WebSocketEncoder.Encode(new PlayerJoinedMessage(gameInfo.Player2!));
        
        //Fire and forget updates to websockets assumes that if a game exists then player1 exists 
        _ = sessionSocketHandler.SendMessageToUsersAsync([gameInfo.Player1.UserId], message);    
    }
    
    //Fire and forget persist to not block the response. 
    _ = persist.SaveGameInfoAsync(gameInfo);

    return Results.Ok();
});

app.MapPost("/TryMakeMove", async (CheckersMoveRequest move) =>
{
    Console.WriteLine($"Trying to make move from {move.GameId} : {move.FromIndex} to {move.ToIndex}");
    if(move.GameId == null)
    {
        return Results.BadRequest("GameId cannot be null");
    }
    if (!gameStateManager.TryGetGameByGameId(move.GameId.Value, out var gameInfo) || gameInfo == null)
    {
       return Results.BadRequest("No active game with the provided Id");
    }
    if (!gameStateManager.TryApplyMove(move.GameId.Value, move.FromIndex, move.ToIndex, out var gameState) || gameState == null)
    {
        return Results.Conflict("Attempted to make an Invalid Move");
    }
    Console.WriteLine($"Move made for game: {move.GameId} from {move.FromIndex} to {move.ToIndex}");
    var latestMove = gameInfo.GameState.GetHistory()[^1];
    var message = WebSocketEncoder.Encode(new GameHistoryUpdateMessage([latestMove]));
    await sessionSocketHandler.SendMessageToUsersAsync(gameInfo.Player2?.UserId == null ? [gameInfo.Player1.UserId] : [gameInfo.Player1.UserId, gameInfo.Player2.UserId], message);
    
    //Fire off a persist operation to not block the response.
    _ = persist.SaveGameInfoAsync(gameInfo);
    
    return Results.Ok();
});


//Web Socket Stuff. 
app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await sessionSocketHandler.AddSocketAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});

//Random GraphQL for fun 
app.MapGraphQL();

app.Run();

record CheckersMoveRequest(Guid? GameId, byte FromIndex, byte ToIndex);
record CreateGameRequest(Guid UserId);
record CreateGameResponse(Guid GameId);
record JoinGameResponse();
record JoinGameRequest(Guid GameId, Guid UserId);