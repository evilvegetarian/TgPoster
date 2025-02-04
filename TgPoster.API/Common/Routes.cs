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
        public const string Delete = Root + "/{id:guid}";
        public const string GetById = Root + "/{id:guid}";
        public const string Get = Root;
        public const string Create = Root;
    }
}