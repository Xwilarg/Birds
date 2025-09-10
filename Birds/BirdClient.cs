using Discord;
using Discord.WebSocket;

namespace Birds;

public class BirdClient
{
    public BirdClient(DiscordSocketClient client, string token)
    {
        _client = client;
        _client.Ready += _client_Ready;

        _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;

        Task.Run(async () => await ConnectAsync(token));
    }

    private Task _client_Ready()
    {
        IsReady = true;
        return Task.CompletedTask;
    }

    private async Task _client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState stateBefore, SocketVoiceState stateAfter)
    {
        if (stateAfter.VoiceChannel != null) await Flock.UpdateVoiceChannelAsync(((IGuildChannel)stateAfter.VoiceChannel).GuildId, stateAfter.VoiceChannel.Id);
        else if (stateBefore.VoiceChannel != null) await Flock.UpdateVoiceChannelAsync(((IGuildChannel)stateBefore.VoiceChannel).GuildId, stateBefore.VoiceChannel.Id);
    }

    public bool IsReady { private set; get; }

    public IEnumerable<IGuild> GetServers()
    {
        return _client.Guilds.Cast<IGuild>();
    }

    public async Task JoinChannelAsync(IVoiceChannel vc)
    {
        await vc.ConnectAsync();
    }

    public async Task LeaveChannelAsync(IVoiceChannel vc)
    {
        await vc.DisconnectAsync();
    }

    private async Task ConnectAsync(string token)
    {
        _client.Log += LogAsync;

        await _client.LoginAsync(TokenType.Bot, token);
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
        Console.Out.WriteLineAsync($"[{_client.CurrentUser?.Username ?? ""}] {msg}");
        Console.ForegroundColor = cc;
        return Task.CompletedTask;
    }

    private DiscordSocketClient _client;
    public Flock Flock { set; get; }
}
