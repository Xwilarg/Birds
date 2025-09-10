using Discord;

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

    public async Task WaitForInitAsync()
    {
        while (!_birds.Any(x => x.IsReady))
        {
            await Task.Delay(1000);
        }
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
            await s.TryDoActionAsync(delay, _rand, _birds);
        }
    }

    public async Task UpdateVoiceChannelAsync(ulong servId, ulong chanId)
    {
        if (_servers.ContainsKey(servId)) await _servers[servId].UpdateUserCountAsync(chanId);
    }

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

        public async Task TryDoActionAsync(int delay, Random rand, IEnumerable<BirdClient> birds)
        {
            _lastAction += delay;

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

            foreach (var b in birds)
            {
                if (rand.Next(10) == 0) // Only 1% chance a bird do something (so 1 every 10 seconds)
                {
                    continue;
                }

                if (BirdTarget == null)
                {
                    var connected = _chans.Values.FirstOrDefault(x => x.ConnectedBirds.Contains(b));
                    if (connected != null)
                    {
                        await connected.DisconnectAsync(b);
                    }
                }
                else
                {
                    var connected = _chans.Values.FirstOrDefault(x => x.ConnectedBirds.Contains(b));
                    if (connected == null)
                    {
                        await _chans[BirdTarget.Value].ConnectAsync(b);
                    }
                    else if (connected.Channel.Id != BirdTarget)
                    {
                        await _chans[connected.Channel.Id].DisconnectAsync(b);
                        await _chans[BirdTarget.Value].ConnectAsync(b);
                    }
                }
            }
        }

        public async Task UpdateUserCountAsync(ulong chanId)
        {
            if (_chans.ContainsKey(chanId)) await _chans[chanId].UpdateUserCountAsync();
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
            UserCount = (await Channel.GetUsersAsync().FlattenAsync()).Count();
        }

        public async Task ConnectAsync(BirdClient client)
        {
            try
            {
                await client.JoinChannelAsync(Channel);
                ConnectedBirds.Add(client);
            }
            catch (Exception ex) { }
        }

        public async Task DisconnectAsync(BirdClient client)
        {
            try
            {
                await client.LeaveChannelAsync(Channel);
                ConnectedBirds.Remove(client);
            }
            catch (Exception ex) { }
        }

        public IVoiceChannel Channel { private set; get; }

        public int UserCount { private set; get; }

        public List<BirdClient> ConnectedBirds { private set; get; } = [];
    }
}
