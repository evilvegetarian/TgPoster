namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

public static class TelegramChanelExtension
{
    public static string ConvertToTelegramHandle(this string name)
    {
        string prefix = "https://t.me/";

        if (name.StartsWith(prefix))
        {
            return string.Concat("@", name.AsSpan(prefix.Length));
        }
        else if (!name.StartsWith('@'))
        {
            return string.Concat("@", name.AsSpan(prefix.Length));
        }

        return name;
    }
}