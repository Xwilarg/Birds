using Discord;
using Discord.WebSocket;

namespace Birds;

public class Flock
{
    public Flock(IList<BirdClient> birds)
    {
        _birds = birds;
        _rand = new();
        foreach (var b in _birds)
        {
            b.Flock = this;
        }
    }

    public async Task ConnectAllAsync()
    {
        foreach (var b in _birds)
        {
            await b.ConnectAsync();
        }
    }

    public async Task WaitForInitAsync()
    {
        while (!_birds.Any(x => x.IsReady))
        {
            await Task.Delay(1000);
        }
    }

    public async Task PerturbAsync(ulong servId, ulong chanId)
    {
        await _servers[servId].PerturbAsync(_rand, chanId, GetBirds(_servers[servId]));
    }

    public async Task InitChannelsAsync()
    {
        _servers = _birds.SelectMany(x => x.GetServers()).DistinctBy(x => x.Id).ToDictionary(x => x.Id, x => new ServerInfo(x));

        foreach (var s in _servers.Values)
        {
            await s.UpdateInfoAsync();
        }
    }

    public async Task TryDoActionAsync(int delay)
    {
        foreach (var s in _servers.Values)
        {
            await s.TryDoActionAsync(delay, _rand, GetBirds(s));
        }
    }

    public async Task UpdateVoiceChannelAsync(ulong servId, ulong chanId)
    {
        if (_servers.ContainsKey(servId)) await _servers[servId].UpdateUserCountAsync(chanId);
    }

    private IEnumerable<BirdClient> GetBirds(ServerInfo s)
        => _birds.Where(x => s.IsInServer(x));

    private IList<BirdClient> _birds;
    private Dictionary<ulong, ServerInfo> _servers = [];
    private Random _rand;

    private class ServerInfo
    {
        public ServerInfo(IGuild g)
        {
            _guild = g;

            _lastAction = 1_000_000;
        }

        public async Task UpdateInfoAsync()
        {
            _chans = (await _guild.GetVoiceChannelsAsync()).ToDictionary(x => x.Id, x => new VoiceChanGoal(x));

            foreach (var c in _chans.Values)
            {
                await c.UpdateInfoAsync();
            }
        }

        public async Task PerturbAsync(Random rand, ulong chanId, IEnumerable<BirdClient> birds)
        {
            Console.WriteLine($"[{_guild.Id} / {_guild.Name}] Someone perturbed the birds!");
            if (BirdTarget == chanId && rand.Next(0, 5) != 0)
            {
                Console.WriteLine($"[{_guild.Id} / {_guild.Name}] Bird were perturbed and their target was unset");
                BirdTarget = null;
            }
            foreach (var b in birds)
            {
                var connected = _chans.Values.FirstOrDefault(x => x.IsConnected(b));
                if (connected != null && connected.Channel.Id == chanId && rand.Next(0, 3) != 0)
                {
                    Console.WriteLine($"[{_guild.Id} / {_guild.Name}] {b} was perturbed and fly away");
                    await b.LeaveChannelAsync(connected.Channel);
                }
            }
        }

        public async Task TryDoActionAsync(int delay, Random rand, IEnumerable<BirdClient> birds)
        {
            _lastAction += delay;

            if (_lastAction < 20) return;

            if (BirdTarget == null)
            {
                if (rand.Next(10) == 0) // Set new flock objective
                {
                    var chansWithUsers = _chans.Where(x => x.Value.UserCount > 0).ToArray();

                    if (chansWithUsers.Any())
                    {
                        int index = rand.Next(chansWithUsers.Length);
                        var e = chansWithUsers[index];
                        BirdTarget = e.Key;
                        Console.WriteLine($"[{_guild.Id} / {_guild.Name}] Setting bird target to {BirdTarget} ({e.Value.Channel.Name})");
                    }
                }
            }
            else
            {
                if (rand.Next(10) == 0 && _chans[BirdTarget.Value].UserCount == 0)
                {
                    BirdTarget = null;
                    Console.WriteLine($"[{_guild.Id} / {_guild.Name}] Unsetting bird target");
                }
            }

            foreach (var b in birds)
            {
                if (rand.Next(10) != 0) // Only 1% chance a bird do something (so 1 every 10 seconds)
                {
                    continue;
                }

                try
                {
                    if (BirdTarget == null)
                    {
                        var connected = _chans.Values.FirstOrDefault(x => x.IsConnected(b));
                        if (connected != null)
                        {
                            await connected.DisconnectAsync(b);
                            _lastAction = 0;
                        }
                    }
                    else
                    {
                        var connected = _chans.Values.FirstOrDefault(x => x.IsConnected(b));
                        if (connected == null)
                        {
                            Console.WriteLine(b);
                            await _chans[BirdTarget.Value].ConnectAsync(b);
                            _lastAction = 0;
                        }
                        else if (connected.Channel.Id != BirdTarget)
                        {
                            await _chans[connected.Channel.Id].DisconnectAsync(b);
                            await _chans[BirdTarget.Value].ConnectAsync(b);
                            _lastAction = 0;
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public async Task UpdateUserCountAsync(ulong chanId)
        {
            if (_chans.ContainsKey(chanId)) await _chans[chanId].UpdateUserCountAsync();
        }

        public bool IsInServer(BirdClient b)
        {
            return b.GetServers().Any(x => x.Id == _guild.Id);
        }

        private int _lastAction;
        private IGuild _guild;
        private Dictionary<ulong, VoiceChanGoal> _chans = [];

        public bool IsReady { private set; get; }

        public ulong? BirdTarget { set; get; }
    }

    private class VoiceChanGoal
    {
        public VoiceChanGoal(IVoiceChannel chan)
        {
            Channel = chan;
        }

        public async Task UpdateInfoAsync()
        {
            var people = await Channel.GetUsersAsync().FlattenAsync();

            await UpdateUserCountAsync();
        }

        public async Task UpdateUserCountAsync()
        {
            UserCount = ((SocketVoiceChannel)Channel).ConnectedUsers.Where(x => !x.IsBot).Count();
            Console.WriteLine($"Refreshing cache for {Channel.GuildId} / {Channel.Id} ({Channel.Name}): {UserCount} user(s) connected");
        }

        public async Task ConnectAsync(BirdClient client)
        {
            Console.Write($"[{client}] Connecting to {Channel.GuildId} / {Channel.Id}");
            try
            {
                await client.JoinChannelAsync(Channel);
                Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task DisconnectAsync(BirdClient client)
        {
            Console.Write($"[{client}] Disconnecting from {Channel.GuildId} / {Channel.Id}");
            try
            {
                await client.LeaveChannelAsync(Channel);
                Console.WriteLine("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public IVoiceChannel Channel { private set; get; }

        public int UserCount { private set; get; }

        public bool IsConnected(BirdClient b)
            => ((SocketVoiceChannel)Channel).ConnectedUsers.Any(x => b.Is(x.Id));
    }
}
