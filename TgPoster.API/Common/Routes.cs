namespace TgPoster.API.Common;

public static class Routes
{
    private const string Root = "api";
    private const string Version = "v1";
    private const string Base = Root + "/" + Version;

    public static class Account
    {
        public const string Root = Base + "/account";
        public const string SignOn = Root + "/sign-on";
        public const string SignIn = Root + "/sign-in";
    }

    public static class Schedule
    {
        public const string Root = Base + "/schedule";
        public const string List = Root;
        public const string Create = Root;
        public const string GetById = Root + "/{id:guid}";
        public const string Delete = Root + "/{id:guid}";
    }

    public static class Day
    {
        public const string Root = Base + "/day";
        public const string DayOfWeek = Root + "/dayofweek";
        public const string UpdateTime = Root + "/time";
        public const string Get = Root;
        public const string Create = Root;
    }

    public static class TelegramBot
    {
        public const string Root = Base + "/telegram-bot";
        public const string List = Root;
        public const string Create = Root;
        public const string GetById = Root + "/{id:guid}";
        public const string Delete = Root + "/{id:guid}";
    }

    public static class Message
    {
        public const string Root = Base + "/message";
        public const string List = Root;
        public const string CreateMessagesFromFiles = Root + "/batch-from-files";
        public const string Create = Root;
        public const string GetById = Root + "/{id:guid}";
        public const string Delete = Root + "/{id:guid}";
    }

    public static class File
    {
        public const string Root = Base + "/file";
        public const string GetById = Root + "/{id:guid}";
    }

    public static class Parse
    {
        public const string Root = Base + "/parse";
        public const string ParseChannel = Root + "/";
    }
}