using Microsoft.AspNetCore.SignalR;
using VandaliaCentral.Hubs;

namespace VandaliaCentral.Game.Tanks;

public sealed class TanksGameLoopService : BackgroundService
{
    private const int TickRate = 30;
    private const int BroadcastEveryTicks = 2;

    private readonly TanksGameEngine _engine;
    private readonly IHubContext<TanksHub> _hubContext;
    private readonly ILogger<TanksGameLoopService> _logger;

    public TanksGameLoopService(TanksGameEngine engine, IHubContext<TanksHub> hubContext, ILogger<TanksGameLoopService> logger)
    {
        _engine = engine;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickDuration = TimeSpan.FromMilliseconds(1000.0 / TickRate);
        var tickCounter = 0;

        using var timer = new PeriodicTimer(tickDuration);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _engine.Tick(1f / TickRate);
                tickCounter++;

                if (tickCounter % BroadcastEveryTicks == 0)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveSnapshot", _engine.CreateSnapshot(), stoppingToken);
                    await _hubContext.Clients.All.SendAsync("ReceiveLobbyState", _engine.CreateLobbyState(), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tanks game loop tick failed.");
            }
        }
    }
}
