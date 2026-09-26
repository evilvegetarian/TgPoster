namespace Shared.CrossPosting;

/// <summary>
///     Результат сборки текста кросс-поста
/// </summary>
/// <param name="Parts"></param>
/// <param name="Truncated"></param>
/// <param name="FellBackToTeaser"></param>
public sealed record CrossPostComposition(IReadOnlyList<string> Parts, bool Truncated, bool FellBackToTeaser);
