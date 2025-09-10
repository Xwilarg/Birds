using Discord;

namespace Birds;

public class Flock
{
    public Flock(IEnumerable<BirdClient> birds)
    {
        _birds = birds;
    }

    public async Task WaitForInitAsync()
    {
        while (_birds.Any(x => !x.IsReady))
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

    public async Task TryDoActionAsync()
    {

    }

    private IEnumerable<BirdClient> _birds;
    private Dictionary<ulong, ServerInfo> _servers = [];

    private class ServerInfo
    {
        public ServerInfo(IGuild g)
        {
            _guild = g;
        }

        public async Task UpdateInfoAsync()
        {
            _chans = (await _guild.GetVoiceChannelsAsync()).ToDictionary(x => x.Id, x => new VoiceChanGoal(x));

            foreach (var c in _chans.Values)
            {
                await c.UpdateInfoAsync();
            }
        }

        private IGuild _guild;
        private Dictionary<ulong, VoiceChanGoal> _chans = [];

        public bool IsReady { private set; get; }
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
        }

        public IVoiceChannel Channel { private set; get; }
    }
}
