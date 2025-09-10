using Discord;
using Discord.WebSocket;
using System.Text.Json;

namespace Birds;

public sealed class Program
{
    private static Flock _flock;

    public static async Task Main()
    {
        if (!File.Exists("credentials.json")) throw new FileNotFoundException("Missing credentials file");

        var creds = JsonSerializer.Deserialize<Credentials>(File.ReadAllText("credentials.json"), new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
        if (creds.Tokens == null || !creds.Tokens.Any()) throw new Exception("Missing bot tokens");

        _flock = new(
            creds.Tokens.Select(x => new BirdClient(new DiscordSocketClient(new()
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            }), x)
        ));

        await _flock.WaitForInitAsync();
        await _flock.InitChannelsAsync();

        while (true)
        {
            await _flock.TryDoActionAsync();
            await Task.Delay(1000);
        }
    }
}