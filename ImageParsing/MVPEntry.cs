using Discord;
using MVPDiscordBot.MDBConstants;

namespace MVPDiscordBot.ImageParsing
{
    internal class MVPEntry(DateTime timeStamp, DateTime? mvpTime, string location, string channel, string originalMessage)
    {
        public DateTime TimeStamp
        {
            get;
            private set;
        } = timeStamp;

        public DateTime? MVPTime
        {
            get;
            private set;
        } = mvpTime;

        public string Location
        {
            get;
            private set;
        } = location ?? throw new ArgumentNullException(nameof(location));

        public string Channel
        {
            get;
            private set;
        } = channel ?? throw new ArgumentNullException(nameof(channel));

        public string OriginalMessage
        {
            get;
            private set;
        } = originalMessage ?? throw new ArgumentNullException(nameof(originalMessage));

        public Embed Embed()
        {
            EmbedBuilder embedBuilder = new();

            embedBuilder.AddField("MVP Time:", $"{(MVPTime == null ? "Unknown" : $"{MVPTime:HH:mm}")}");
            embedBuilder.AddField("Server time posted:", $"{TimeStamp:HH:mm}");
            embedBuilder.AddField("Location:", Location);
            embedBuilder.AddField("Channel:", Channel);
            embedBuilder.AddField("Interpreted message:", OriginalMessage);
            embedBuilder.WithImageUrl("attachment://" + Path.GetFileName(Constants.FULLSCREENSHOTPATH));

            return embedBuilder.Build();
        }

        public override bool Equals(object? obj)
        {
            return obj is MVPEntry entry &&
                TimeStamp == entry.TimeStamp &&
                MVPTime == entry.MVPTime;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TimeStamp, MVPTime);
        }

        public override string ToString()
        {
            return $"Server time posted: {TimeStamp:HH:mm}\n" +
                $"MVP Time: {(MVPTime == null ? "Unknown" : $"{MVPTime:HH:mm}")}\n" +
                $"Location: {Location}\n" +
                $"Channel: {Channel}\n" +
                $"Interpreted message: {OriginalMessage}";
        }
    }
}
