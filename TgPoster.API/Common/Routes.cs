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
        internal const string Get = Root + "/{id:guid}";
        internal const string Delete = Root + "/{id:guid}";
        internal const string UpdateStatus = Root + "/{id:guid}/status";
    }

    internal static class Day
    {
        internal const string Root = Base + "/day";
        internal const string DayOfWeek = Root + "/day-of-week";
        internal const string UpdateTime = Root + "/time";
        internal const string GetBySchedule = Root;
        internal const string Create = Root;
    }

    internal static class TelegramBot
    {
        internal const string Root = Base + "/telegram-bot";
        internal const string List = Root;
        internal const string Create = Root;
        internal const string Get = Root + "/{id:guid}";
        internal const string Delete = Root + "/{id:guid}";
        internal const string Update = Root + "/{id:guid}";
    }

    internal static class Message
    {
        internal const string Root = Base + "/message";
        internal const string List = Root;
        internal const string CreateMessagesFromFiles = Root + "/batch-from-files";
        internal const string Create = Root;
        internal const string Update = Root + "/{id:guid}";
        internal const string DeleteFileMessage = Root + "/{id:guid}/files/{fileId:guid}";
        internal const string LoadFiles = Root + "/{id:guid}/file";
        internal const string ApproveMessages = Root;
        internal const string Get = Root + "/{id:guid}";
        internal const string Delete = Root;
        internal const string GetTime = Root + "/{scheduleId:guid}/time";
    }

    internal static class File
    {
        internal const string Root = Base + "/file";
        internal const string Get = Root + "/{id:guid}";
    }

    internal static class ParseChannel
    {
        internal const string Root = Base + "/parse-channel";
        internal const string Create = Root;
        internal const string List = Root;
        internal const string Update = Root + "/{id:guid}";
    }
}