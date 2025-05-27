using System.Diagnostics;
using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers;

namespace WebGameServer.GameStateManagement.Timers;

public class GameTimer(uint timerId, TimeSpan tickInterval)
{
    private readonly Stopwatch _stopwatch = new();
    public long elapsedTimeMs => _stopwatch.ElapsedMilliseconds;
    private long _lastTickMs;
    public long GetDeltaTime() => _stopwatch.ElapsedMilliseconds - _lastTickMs;
    public async Task Start(TimeSpan? startDelay = null, CancellationToken cancellationToken = default)
    {
        if (startDelay != null)
        {
            await Task.Delay(startDelay.Value, cancellationToken);
        }
        _stopwatch.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(tickInterval, cancellationToken);
            await OnTimerTick(timerId);
            _lastTickMs = _stopwatch.ElapsedMilliseconds;
        }

        _stopwatch.Stop();
        _stopwatch.Reset();
    }
    
    private static async Task OnTimerTick(uint timerId)
    {
        await LocalGameSpace.TimerTick(timerId, OnGameTimeout);
    }

    private static void OnGameTimeout(GameInfo game)
    {
        var idsToSend = game.GetNonNullPlayerIds();
        var userSessions = SessionSocketHandler.GetSessionsForPlayers(idsToSend);
        foreach (var userSession in userSessions)
        {
            userSession.GameId = GameInfo.EmptyGameId;
        }
        _ = WebSocketWriter.WriteGameStatusUpdate(userSessions, game.Status); 
    }
}


public static class GameTimers
{
    public const  uint TimerAmount = 8;
    private const uint TimerIntervalMs = 300;
    private const uint TimerOffset = TimerIntervalMs / TimerAmount;  
        
    private static readonly GameTimer[] Timers = new GameTimer[TimerAmount];
    
    private static readonly CancellationTokenSource[] CancellationTokens = new CancellationTokenSource[TimerAmount];
    
    public static void InitializeTimers()
    {
        for (uint i = 0; i < TimerAmount; i++)
        {
            var startDelay = TimerOffset * i;
            Timers[i] = new GameTimer(i, TimeSpan.FromMilliseconds(TimerIntervalMs));
            CancellationTokens[i] = new CancellationTokenSource();
            
            //Fire and Forget, as these are background timers which live for the lifetime of the server. 
            _ = Timers[i].Start(TimeSpan.FromMilliseconds(startDelay), CancellationTokens[i].Token); 
        }
    }
    public static void StopTimers()
    {
        for (uint i = 0; i < TimerAmount; i++)
        {
            CancellationTokens[i].Cancel();
            CancellationTokens[i].Dispose(); ;
        }
    }
    public static GameTimer GetTimer(uint timerId)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(timerId, TimerAmount);
        return Timers[timerId];
    }
}