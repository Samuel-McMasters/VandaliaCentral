using Microsoft.AspNetCore.SignalR;
using VandaliaCentral.Game.Tanks;
using VandaliaCentral.Models.Tanks;

namespace VandaliaCentral.Hubs;

public sealed class TanksHub : Hub
{
    private readonly TanksGameEngine _engine;

    public TanksHub(TanksGameEngine engine)
    {
        _engine = engine;
    }

    public async Task JoinGame(string name)
    {
        if (_engine.JoinGame(Context.ConnectionId, name, out var error))
        {
            await Clients.Caller.SendAsync("ReceiveJoined", Context.ConnectionId);
            await Clients.All.SendAsync("ReceiveLobbyState", _engine.CreateLobbyState());
            return;
        }

        await Clients.Caller.SendAsync("ReceiveJoinError", error ?? "Unable to join game.");
    }

    public async Task LeaveGame()
    {
        _engine.LeaveGame(Context.ConnectionId);
        await Clients.All.SendAsync("ReceiveLobbyState", _engine.CreateLobbyState());
    }

    public Task SendInput(InputDto input)
    {
        _engine.ApplyInput(Context.ConnectionId, input);
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _engine.LeaveGame(Context.ConnectionId);
        await Clients.All.SendAsync("ReceiveLobbyState", _engine.CreateLobbyState());
        await base.OnDisconnectedAsync(exception);
    }
}
