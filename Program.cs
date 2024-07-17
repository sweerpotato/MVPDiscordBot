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
        private static DiscordClient? _DiscordClient;
        
        public static async Task Main()
        {
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
                string currentFullScreenshotPath = Constants.FULLSCREENSHOTPATH;
                Bitmap? screenCapture = ScreenCapture.CaptureMaplestoryChat();

                if (screenCapture != null)
                {
                    screenCapture.Save(currentFullScreenshotPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    await Task.Delay(5000);
                    continue;
                }

                //Uncomment to debug
                //currentFullScreenshotPath = "C:\\Users\\sebas\\Desktop\\testmvpbot\\maplechat2.png";

                try
                {
                    IEnumerable<string> chatMessages = ChatParser.ParseChatImage(currentFullScreenshotPath);

                    foreach (MVPEntry mvp in ChatParser.FilterMVPs(chatMessages))
                    {
                        if (!mvpCache.Contains(mvp))
                        {
                            await _DiscordClient.SendMessage(mvp);
                            mvpCache.Add(mvp);
                        }
                    }
                    //await _DiscordClient.SendMessage(new MVPEntry(DateTime.Now, DateTime.Now, "Henesys", "Channel 1", "BABABA"));
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