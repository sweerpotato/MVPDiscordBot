using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MVPDiscordBot.ImageParsing;

namespace MVPDiscordBot.Discord
{
    internal class DiscordClient
    {
        private readonly DiscordSocketClient _Client = new();
        private readonly EventWaitHandle _ClientReady = new(false, EventResetMode.ManualReset);

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());

            return Task.CompletedTask;
        }

        public async Task Connect(string token)
        {
            _Client.Log += Log;
            _Client.Ready += OnClientReady;

            await _Client.LoginAsync(TokenType.Bot, token);
            await _Client.StartAsync();
        }

        public async Task Disconnect()
        {
            await _Client.StopAsync();
        }

        public async Task SendMessage(MVPEntry mvpEntry)
        {
            //SocketGuild guild = _Client.GetGuild(1253041928765702268);
            //IReadOnlyCollection<RestThreadChannel> threads = await guild.
            //    GetTextChannel(1253042028535742486).
            //    GetActiveThreadsAsync();
            _ClientReady.WaitOne();

            SocketGuild guild = _Client.GetGuild(405323062859530240);
            SocketTextChannel textChannel = guild.GetTextChannel(909504001765150801);
            IReadOnlyCollection<RestThreadChannel> threads = await textChannel.GetActiveThreadsAsync();
            int waitCount = 0;

            while (threads == null)
            {
                if (waitCount == 3)
                {
                    throw new Exception($"Could not fetch {nameof(threads)}");
                }

                ++waitCount;
                Console.WriteLine($"Waiting for threads attempt {waitCount}..");
                await Task.Delay(3000);
                threads = await textChannel.GetActiveThreadsAsync();
            }

            RestThreadChannel? thread = threads.SingleOrDefault(thread => thread.Name == "MVPs");
            SocketRole mvpRole = guild.Roles.First(role => role.Name == "MVP");

            if (thread == null)
            {
                throw new NullReferenceException($"{nameof(thread)} can't be found");
            }

            await thread.SendMessageAsync(mvpRole.Mention, false, mvpEntry.Embed());
        }

        private Task OnClientReady()
        {
            _ClientReady.Set();

            return Task.CompletedTask;
        }
    }
}
