namespace WerkonWebServicesRatchet.Web.Localization;

public sealed record LanguageOption(string Code, string NativeName, string CultureName)
{
    public static LanguageOption Russian { get; } = new("ru", "Русский", "ru-RU");
    public static LanguageOption English { get; } = new("en", "English", "en-US");
    public static LanguageOption Japanese { get; } = new("ja", "日本語", "ja-JP");

    public static IReadOnlyList<LanguageOption> All { get; } =
    [
        Russian,
        English,
        Japanese
    ];
}
