namespace TgPoster.Endpoint.Tests;

internal static class GlobalConst
{
	public static readonly Guid TelegramBotId = Guid.Parse("b22ed178-4bae-49bf-b25b-74c418f2b669");
	public static readonly Guid ScheduleId = Guid.Parse("6ad3cf92-01bd-4c53-ac26-be2b254b0e3a");
	public static readonly Guid MessageId = Guid.Parse("71aa5579-42cb-43bc-93fd-f9ceba7c562f");
	public static readonly Guid UserId = Guid.Parse("ae35ca81-4a8a-4f8c-a2bb-f4dca7a56876");
	public static readonly Guid FileId = Guid.Parse("bfddea1f-9110-46a3-bae6-9ee358b7fb99");
	public static readonly Guid UserIdEmpty = Guid.Parse("d312dedb-70e8-4015-8d88-dc3c4d49f30c");
	public static readonly long ChannelId = -1001563514057;

	public static class Worked
	{
		public static readonly Guid TelegramBotId = Guid.Parse("55f1dee4-137d-4ade-9f91-84249aceaec4");
		public static readonly Guid ScheduleId = Guid.Parse("cee7c166-b5cf-41b4-8c66-d470a4974000");
		public static readonly Guid UserId = Guid.Parse("b6cbe54a-21d2-44d5-bfcc-a9f93e3fc93c");
		public static readonly long ChannelId = -1002233268199;
		public static readonly long ChatIdTg = 492807650;
		public static readonly string Channel = "https://t.me/kotiksuper";
		public static readonly string UserName = "Default_User_For_APP";
		public static readonly string Password = "string";
		public static readonly string Model = "openai/gpt-3.5-turbo-0613";
	}
}