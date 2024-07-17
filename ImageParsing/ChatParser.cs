using MVPDiscordBot.MDBConstants;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Tesseract;

namespace MVPDiscordBot.ImageParsing
{
    internal static partial class ChatParser
    {
        /// <summary>
        /// Matches some indicators of an MVP being casted
        /// </summary>
        [GeneratedRegex(@"(mvp|\d/\d|\sxx:\d\d|\sxx\d\d|\sx\d\d|\sx:\d\d|extend)")]
        private static partial Regex MVPIndicatorRegex();

        /// <summary>
        /// Matches some variations of different MVP timestamps - xx:00 etc
        /// </summary>
        [GeneratedRegex(@"(x+:?\d\d)/?(x+:?\d\d)?")]
        private static partial Regex MVPTimeStampRegex();

        /// <summary>
        /// Matches chat messages starting with [dd:dd] including newlines until next non-inclusive [dd:dd]
        /// </summary>
        [GeneratedRegex(@"\[\d{2}:\d{2}\](.|\r|\n)*?(?=(\[\d{2}:\d{2}\]|\z))")]
        private static partial Regex ChatMessageRegex();

        /// <summary>
        /// Matches time stamps at the start of chat messages as HH:mm
        /// </summary>
        [GeneratedRegex(@"^\[(\d{2}:\d{2})\]")]
        private static partial Regex TimeStampRegex();

        /// <summary>
        /// Matches channel numbers by looking for "ch", "channel" or "cc" followed by one or two numbers
        /// </summary>
        [GeneratedRegex(@"(c|ch|cc|channel)\D?(\d{1,2})")]
        private static partial Regex ChannelRegex();

        /// <summary>
        /// Parses the text from the smega chat from a chat picture specified by <paramref name="imageFullPath"/>
        /// </summary>
        /// <param name="imageFullPath">Full path to the chat image to process</param>
        /// <returns>The parsed chat messages</returns>
        public static IEnumerable<string> ParseChatImage(string imageFullPath)
        {
            string[] ocrText;

            Dictionary<string, object> initOptions = new()
            {
                { "user_words_suffix", "user-words" },
                { "user_patterns_suffix", "user-patterns" },
                { "language_model_penalty_non_freq_dict_word", 1 },
                { "language_model_penalty_non_dict_word", 1 }
            };

            using (TesseractEngine engine = new(Constants.TESSDATA_PATH, "eng", EngineMode.Default, [], initOptions, false))
            {
                using Pix pix = Pix.LoadFromFile(imageFullPath);
                using Page page = engine.Process(pix.ConvertRGBToGray(0.2f, 0.7f, 0.1f).Scale(2f, 2f));

                ocrText = page.GetText().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            Regex chatMessageRegex = ChatMessageRegex();

            //Groups all chat messages independent of newlines
            foreach (Match match in chatMessageRegex.Matches(String.Join("", ocrText)))
            {
                yield return match.Value;
            }
        }

        /// <summary>
        /// Filters out relevant messages concerning MVPs from <paramref name="chatMessages"/>
        /// </summary>
        /// <param name="chatMessages">The chat messages to filter</param>
        public static IEnumerable<MVPEntry> FilterMVPs(IEnumerable<string> chatMessages)
        {
            Regex mvpIndicatorRegex = MVPIndicatorRegex();
            Regex timeStampRegex = TimeStampRegex();
            Regex channelRegex = ChannelRegex();
            Regex mvpTimeStampRegex = MVPTimeStampRegex();
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.Local;

            foreach (string chatMessage in chatMessages.
                Where(message => mvpIndicatorRegex.IsMatch(message.ToLower()) &&
                    mvpIndicatorRegex.Matches(message.ToLower()).Count > 1 &&
                    !message.Contains("mpe", StringComparison.CurrentCultureIgnoreCase)))
            {
                string lowerChatMessage = chatMessage.ToLower();
                Match timeStampRegexMatch = timeStampRegex.Match(lowerChatMessage);
                DateTime serverTimeStamp = DateTime.SpecifyKind(
                    DateTime.ParseExact(timeStampRegexMatch.Groups[1].Value, "HH:mm", CultureInfo.InvariantCulture),
                    DateTimeKind.Utc);
                DateTime convertedTimeStamp = serverTimeStamp.ToLocalTime();

                if (!timeStampRegexMatch.Success)
                {
                    continue;
                }

                Match mvpTimeStampMatch = mvpTimeStampRegex.Match(lowerChatMessage[7..]);
                string mvpTimeStampString = "Unknown";
                DateTime? mvpTimeStamp = null;

                if (mvpTimeStampMatch.Success)
                {
                    mvpTimeStampString = mvpTimeStampMatch.Value;

                    if (!mvpTimeStampString.Contains(':'))
                    {
                        mvpTimeStampString = mvpTimeStampString.Insert(mvpTimeStampString.LastIndexOf('x') + 1, ":");
                    }

                    int extraHours = 0;

                    if (mvpTimeStampString.Substring(mvpTimeStampString.Length - 2, 2) == "00")
                    {
                        extraHours = 1;
                    }

                    int hours = convertedTimeStamp.Hour + extraHours;

                    if (!mvpTimeStampString.Contains("xx") && mvpTimeStampString.Contains('x'))
                    {
                        mvpTimeStampString = mvpTimeStampString.Insert(mvpTimeStampString.IndexOf('x'), "x");
                    }

                    mvpTimeStampString = mvpTimeStampString.Replace("xx", hours < 10 ? "0" + hours.ToString() : hours.ToString());

                    try
                    {
                        mvpTimeStamp = DateTime.ParseExact(mvpTimeStampString, "HH:mm", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        Console.WriteLine($"Could not parse {nameof(mvpTimeStamp)} with " +
                            $"{nameof(mvpTimeStampString)}: {mvpTimeStampString}" +
                            $" and message {lowerChatMessage}");
                    }
                }
                else if (!lowerChatMessage.Contains("extend"))
                {
                    continue;
                }

                Match channelRegexMatch = channelRegex.Match(lowerChatMessage);
                string channel = "Unknown";

                //Can be fine if the channel doesn't match. May be MVP extension or something
                if (channelRegexMatch.Success)
                {
                    channel = channelRegexMatch.Groups[2].Value.TrimStart('0');
                }

                string location;

                if (lowerChatMessage.Contains("kerning") ||
                    lowerChatMessage.Contains("kernig") ||
                    lowerChatMessage.Contains("kering") ||
                    lowerChatMessage.Contains("city"))
                {
                    location = "Kerning City";
                }
                else if (lowerChatMessage.Contains("hene"))
                {
                    location = "Henesys";
                }
                else if (lowerChatMessage.Contains("leafre"))
                {
                    location = "Leafre";
                }
                else if (lowerChatMessage.Contains("cern"))
                {
                    location = "Cernium";
                }
                else if (lowerChatMessage.Contains("ludi"))
                {
                    location = "Ludibrium (what the fuck)";
                }
                else if (lowerChatMessage.Contains("ellinia"))
                {
                    location = "Ellinia";
                }
                else if (lowerChatMessage.Contains("nameless") || lowerChatMessage.Contains("vanishing"))
                {
                    location = "Nameless Town";
                }
                else if (lowerChatMessage.Contains("lith harbor"))
                {
                    location = "Lith Harbor";
                }
                else
                {
                    location = "Unknown";
                }
                
                DateTime combinedTimeStamp = DateTime.Today.AddHours(serverTimeStamp.Hour).AddMinutes(serverTimeStamp.Minute);

                yield return new MVPEntry(combinedTimeStamp, mvpTimeStamp, location, channel, chatMessage);
            }
        }
    }
}
