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
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates
            }), x)
        ).ToList());

        await _flock.ConnectAllAsync();
        Console.WriteLine("Waiting for birds to be ready");
        await _flock.WaitForInitAsync();
        Console.WriteLine("Initializing birds data");
        await _flock.InitChannelsAsync();
        Console.WriteLine("Birds are ready");

        while (true)
        {
            await _flock.TryDoActionAsync(1000);
            await Task.Delay(1000);
        }
    }
}