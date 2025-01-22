namespace TgPoster.API.Common;

public static class Routes
{
    private const string Root = "api";
    private const string Version = "v1";
    private const string Base = Root + "/" + Version;

    public static class Account
    {
        public const string Root = Base + "/account";
        public const string GetById = Root + "/{id:guid}";
    }
    
}