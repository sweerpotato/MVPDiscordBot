using Discord;

namespace MVPDiscordBot.ImageParsing
{
    internal class MVPEntry(DateTime timeStamp, string mvpTime, string location, string channel, string originalMessage)
    {
        public DateTime TimeStamp
        {
            get;
            private set;
        } = timeStamp;

        public string MVPTime
        {
            get;
            private set;
        } = mvpTime ?? throw new ArgumentNullException(nameof(mvpTime));

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

            embedBuilder.AddField("Server time posted:", $"{TimeStamp:HH:mm}");
            embedBuilder.AddField("MVP Time:", MVPTime);
            embedBuilder.AddField("Location:", Location);
            embedBuilder.AddField("Channel:", Channel);
            embedBuilder.AddField("Interpreted message:", OriginalMessage);

            return embedBuilder.Build();
        }

        public override bool Equals(object? obj)
        {
            return obj is MVPEntry entry &&
                TimeStamp == entry.TimeStamp &&
                MVPTime == entry.MVPTime &&
                Location == entry.Location &&
                Channel == entry.Channel;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TimeStamp, MVPTime, Location, Channel);
        }

        public override string ToString()
        {
            return $"Server time posted: {TimeStamp:HH:mm}\nMVP Time: {MVPTime}\nLocation: {Location}\nChannel: {Channel}\nInterpreted message: {OriginalMessage}";
        }
    }
}
