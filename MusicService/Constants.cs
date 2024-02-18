namespace MusicService;

public static class Constants
{
    public static class Files
    {
        public const string DatabaseFileName = ".music.db";
    }

    public static class Types
    {
        public const string Flac = "flac";

        public const string Aac = "aac";

        // ReSharper disable once InconsistentNaming
        public const string M4a = "m4a";

        public const string Mp3 = "mp3";

        public const string Ogg = "ogg";

        public static string[] AllTypes => [Flac, Aac, M4a, Mp3, Ogg];
    }
}