using System.Net.WebSockets;
using System.Text;
using WebApplication1;

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

//Game Memory
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

app.UseHttpsRedirection();

app.MapPost("/TryMakeMove", async (CheckersMove move) =>
{
    var validMove = game.TryMove((move.FromX, move.FromY), (move.ToX, move.ToY), out var _gameState);

    //if (validMove)
    //{
    var messageToSend = _gameState.ToJson();
    await WebSocketHandler.SendMessageToAll(messageToSend); 
    //}
    
    return Results.Ok();
});


app.Map("/ws", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        // Accept the WebSocket request
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        WebSocketHandler.AddSocket(webSocket);
        
        var gameStateJson = game._gameState.ToJson();
        var buffer = Encoding.UTF8.GetBytes(gameStateJson);
        var segment = new ArraySegment<byte>(buffer);
        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        
        
        await HandleWebSocketCommunication(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400; // Bad Request
    }
});

// A method to handle WebSocket communication (for example, echoing messages)
async Task HandleWebSocketCommunication(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    
    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        
        if (result.MessageType == WebSocketMessageType.Close)
        {
            // Close the WebSocket connection if requested
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the client", CancellationToken.None);
        }
        else
        {
            // Send the received message back to the client (echo)
            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

app.Run();
record CheckersMove(int FromX, int FromY, int ToX, int ToY);