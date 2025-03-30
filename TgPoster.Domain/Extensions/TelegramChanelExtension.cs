namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

public static class TelegramChanelExtension
{
    public static string ConvertToTelegramHandle(this string name)
    {
        var prefix = "https://t.me/";

        if (name.StartsWith(prefix))
        {
            return string.Concat("@", name.AsSpan(prefix.Length));
        }

        if (!name.StartsWith('@'))
        {
            return string.Concat("@", name.AsSpan(prefix.Length));
        }

        return name;
    }
}