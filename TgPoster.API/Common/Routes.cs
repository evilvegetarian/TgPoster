namespace TgPoster.API.Common;

internal static class Routes
{
    private const string Root = "api";
    private const string Version = "v1";
    private const string Base = Root + "/" + Version;

    internal static class Account
    {
        internal const string Root = Base + "/account";
        internal const string SignOn = Root + "/sign-on";
        internal const string SignIn = Root + "/sign-in";
        internal const string RefreshToken = Root + "/refresh-token";
    }

    internal static class Schedule
    {
        internal const string Root = Base + "/schedule";
        internal const string List = Root;
        internal const string Create = Root;
        internal const string GetById = Root + "/{id:guid}";
        internal const string Delete = Root + "/{id:guid}";
        internal const string UpdateStatus = Root + "/{id:guid}/status";
    }

    internal static class Day
    {
        internal const string Root = Base + "/day";
        internal const string DayOfWeek = Root + "/dayofweek";
        internal const string UpdateTime = Root + "/time";
        internal const string Get = Root;
        internal const string Create = Root;
    }

    internal static class TelegramBot
    {
        internal const string Root = Base + "/telegram-bot";
        internal const string List = Root;
        internal const string Create = Root;
        internal const string GetById = Root + "/{id:guid}";
        internal const string Delete = Root + "/{id:guid}";
    }

    internal static class Message
    {
        internal const string Root = Base + "/message";
        internal const string List = Root;
        internal const string CreateMessagesFromFiles = Root + "/batch-from-files";
        internal const string Create = Root;
        internal const string GetById = Root + "/{id:guid}";
        internal const string Delete = Root + "/{id:guid}";
    }

    internal static class File
    {
        internal const string Root = Base + "/file";
        internal const string GetById = Root + "/{id:guid}";
    }

    internal static class Parse
    {
        internal const string Root = Base + "/parse";
        internal const string ParseChannel = Root ;
        internal const string List = Root;
    }
}