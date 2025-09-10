using Discord;
using Discord.WebSocket;

namespace Birds;

public class BirdClient
{
    public BirdClient(DiscordSocketClient client, string token)
    {
        _client = client;

        _rand = new();

        Task.Run(async () => await ConnectAsync(token));
    }

    public bool IsReady => _client.ConnectionState == ConnectionState.Connected;

    public IEnumerable<IGuild> GetServers()
    {
        return _client.Guilds.Cast<IGuild>();
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
    private Random _rand;
}
