using MVPDiscordBot.Discord;
using MVPDiscordBot.ImageParsing;
using MVPDiscordBot.MDBConstants;
using MVPDiscordBot.ScreenCapturing;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DiscordBot
{
    [SupportedOSPlatform("windows")]
    public partial class Program
    {
        private static readonly string FILEPATH = Environment.GetEnvironmentVariable("SCREENSHOT_DIRECTORY", EnvironmentVariableTarget.User)
            ?? throw new Exception("SCREENSHOT_DIRECTORY missing from environment variables");
        private static readonly string FULLSCREENSHOTPATH = Path.Combine(FILEPATH, "chat.png");
        private static DiscordClient? _DiscordClient;
        
        public static async Task Main()
        {
            await Task.Delay(3000);

            if (!Path.Exists(Constants.TESSDATA_PATH))
            {
                Directory.CreateDirectory(Constants.TESSDATA_PATH);
            }

            HashSet<MVPEntry> mvpCache = [];

            string? token = Environment.GetEnvironmentVariable("FUNGUS_TOKEN", EnvironmentVariableTarget.User) ??
                throw new Exception("FUNGUS_TOKEN missing from environment variables");

            _DiscordClient = new DiscordClient();
            await _DiscordClient.Connect(token);

            while (true)
            {
                ScreenCapture.CaptureMaplestoryChat().Save(FULLSCREENSHOTPATH, System.Drawing.Imaging.ImageFormat.Png);

                try
                {
                    IEnumerable<string> chatMessages = ChatParser.ParseChatImage(FULLSCREENSHOTPATH);

                    foreach (MVPEntry mvp in ChatParser.FilterMVPs(chatMessages))
                    {
                        if (!mvpCache.Contains(mvp))
                        {
                            await _DiscordClient.SendMessage(mvp);
                            mvpCache.Add(mvp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine();
                }

                await Task.Delay(5000);
            }
        }
    }
}