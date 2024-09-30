using System.Text.RegularExpressions;
using AbsoluteBot.Chat.Context;
using AbsoluteBot.Services;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
///     Команда для получения перевода текст на русский, или с русского на английский.
/// </summary>
public partial class TranslateCommand(TranslationService translationService) : BaseCommand, IParameterized
{
    public override int Priority => 10;
    public override string Description => "переводит данный текст на русский язык, или если он и так на русском, то на английский.";
    public override string Name => "!переведи";
    public string Parameters => "текст";

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        var targetLanguage = CyrillicRegex().IsMatch(command.Parameters) ? "EN" : "RU";

        return await translationService.TranslateTextAsync(command.Parameters, targetLanguage).ConfigureAwait(false) ??
               "Не могу понять, тут что-то на эльфийском.";
    }

    [GeneratedRegex(@"\p{IsCyrillic}")]
    private static partial Regex CyrillicRegex();
}