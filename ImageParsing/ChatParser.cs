using MVPDiscordBot.MDBConstants;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Tesseract;

namespace MVPDiscordBot.ImageParsing
{
    internal static partial class ChatParser
    {
        //TODO SK: Make better. Stop "mpe"
        /// <summary>
        /// Matches some indicators of an MVP being casted
        /// </summary>
        [GeneratedRegex(@"(mvp|\d/\d|xx:\d\d|xx\d\d|extend)")]
        private static partial Regex MVPIndicatorRegex();

        /// <summary>
        /// Matches some variations of different MVP timestamps - xx:00 etc
        /// </summary>
        [GeneratedRegex(@"(xx:?\d\d)/?(xx:?\d\d)?")]
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

            bool startFound = false;
            int smegaStartIndex = 0;
            int smegaEndIndex = 0;

            foreach (string line in ocrText)
            {
                ++smegaEndIndex;

                if (!startFound && line != "Mega")
                {
                    ++smegaStartIndex;

                    continue;
                }
                else if (line == "Mega")
                {
                    ++smegaStartIndex;
                    startFound = true;

                    continue;
                }

                bool breakCondition = line.Contains("All Party Friend Guild Alliance") ||
                    line.Contains("All Party") ||
                    line.Contains("Party Friend") ||
                    line.Contains("Friend Guild") ||
                    line.Contains("Guild Alliance");

                if (breakCondition)
                {
                    break;
                }
            }

            if (smegaStartIndex == smegaEndIndex)
            {
                yield return String.Empty;
            }

            string concatText = String.Join("", ocrText.Skip(smegaStartIndex).Take(smegaEndIndex - smegaStartIndex - 1));
            //Groups all chat messages independent of newlines
            Regex chatMessageRegex = ChatMessageRegex();

            foreach (Match match in chatMessageRegex.Matches(concatText))
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

            foreach (string chatMessage in chatMessages.
                Where(message => mvpIndicatorRegex.IsMatch(message.ToLower()) &&
                    !message.Contains("mpe", StringComparison.CurrentCultureIgnoreCase)))
            {
                string lowerChatMessage = chatMessage.ToLower();
                Match timeStampRegexMatch = timeStampRegex.Match(lowerChatMessage);

                if (!timeStampRegexMatch.Success)
                {
                    continue;
                }

                Match mvpTimeStampMatch = mvpTimeStampRegex.Match(lowerChatMessage);
                string mvpTimeStamp = "Unknown";

                if (mvpTimeStampMatch.Success)
                {
                    mvpTimeStamp = mvpTimeStampMatch.Value;
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
                else if (lowerChatMessage.Contains("nameless"))
                {
                    location = "Nameless Town";
                }
                else if (lowerChatMessage.Contains("vanishing"))
                {
                    location = "Nameless Town";
                }
                else
                {
                    location = "Unknown";
                }
                
                DateTime chatTimeStamp = DateTime.ParseExact(timeStampRegexMatch.Groups[1].Value, "HH:mm", CultureInfo.InvariantCulture);
                DateTime combinedTimeStamp = DateTime.Today.AddHours(chatTimeStamp.Hour).AddMinutes(chatTimeStamp.Minute);

                yield return new MVPEntry(combinedTimeStamp, mvpTimeStamp, location, channel, chatMessage);
            }
        }
    }
}
