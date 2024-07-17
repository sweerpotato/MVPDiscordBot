namespace MVPDiscordBot.MDBConstants
{
    internal static class Constants
    {
        //TODO: Make path relative to the solution's tessdata
        public static readonly string TESSDATA_PATH = "";

        private static readonly string FILEPATH = Environment.GetEnvironmentVariable("SCREENSHOT_DIRECTORY", EnvironmentVariableTarget.User) ??
            throw new Exception("SCREENSHOT_DIRECTORY missing from environment variables");

        public static readonly string FULLSCREENSHOTPATH = Path.Combine(FILEPATH, "chat.png");
    }
}
