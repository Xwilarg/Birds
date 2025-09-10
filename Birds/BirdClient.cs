using Discord;
using Discord.WebSocket;

namespace Birds;

public class BirdClient
{
    public BirdClient(DiscordSocketClient client, string token)
    {
        _token = token;
        _client = client;
        _client.Ready += _client_Ready;

        _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
    }

    private Task _client_Ready()
    {
        IsReady = true;
        return Task.CompletedTask;
    }

    private async Task _client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState stateBefore, SocketVoiceState stateAfter)
    {
        if (user.IsBot) return;

        if (stateAfter.VoiceChannel != null)
        {
            var id = ((IGuildChannel)stateAfter.VoiceChannel).GuildId;
            await Flock.UpdateVoiceChannelAsync(id, stateAfter.VoiceChannel.Id);
            await Flock.PerturbAsync(id, stateAfter.VoiceChannel.Id);
        }
        else if (stateBefore.VoiceChannel != null)
        {
            var id = ((IGuildChannel)stateBefore.VoiceChannel).GuildId;
            await Flock.UpdateVoiceChannelAsync(id, stateBefore.VoiceChannel.Id);
            await Flock.PerturbAsync(id, stateBefore.VoiceChannel.Id);
        }
    }

    public bool IsReady { private set; get; }

    public IEnumerable<IGuild> GetServers()
    {
        return _client.Guilds.Cast<IGuild>();
    }

    public async Task JoinChannelAsync(IVoiceChannel vc)
    {
        await ((IVoiceChannel)_client.GetChannel(vc.Id)).ConnectAsync();
    }

    public async Task LeaveChannelAsync(IVoiceChannel vc)
    {
        await ((IVoiceChannel)_client.GetChannel(vc.Id)).DisconnectAsync();
    }

    public async Task ConnectAsync()
    {
        _client.Log += LogAsync;

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    private Task LogAsync(LogMessage msg)
    {
        var cc = Console.ForegroundColor;
        Console.ForegroundColor = msg.Severity switch
        {
            LogSeverity.Critical => ConsoleColor.DarkRed,
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Warning => ConsoleColor.DarkYellow,
            LogSeverity.Info => ConsoleColor.White,
            LogSeverity.Verbose => ConsoleColor.Green,
            LogSeverity.Debug => ConsoleColor.DarkGreen,
            _ => throw new NotImplementedException("Invalid log level " + msg.Severity)
        };
        Console.WriteLine($"[{_client.CurrentUser?.Username ?? ""}] {msg}");
        Console.ForegroundColor = cc;
        return Task.CompletedTask;
    }

    public bool Is(ulong id) => _client.CurrentUser.Id == id;

    public override string ToString()
    {
        return _client.CurrentUser.Username;
    }

    private DiscordSocketClient _client;
    public Flock Flock { set; get; }
    private string _token;
}
