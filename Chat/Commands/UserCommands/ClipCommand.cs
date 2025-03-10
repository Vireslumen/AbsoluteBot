using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;
using AbsoluteBot.Services;
using AbsoluteBot.Services.ChatServices.TwitchChat;
using AbsoluteBot.Services.NeuralNetworkServices;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
/// Команда для создания клипа на Twitch. Клип создаётся без названия длинной в 30 секунд.
/// </summary>
public class ClipCommand(TwitchChatService twitchChatService, AskGeminiService geminiService, ClipsService clipsService) : BaseCommand, IParameterized
{
    public override int Priority => 10;
    public override string Description => "делает клип со стрима на twitch или ищет уже сделанный по описанию.";
    public override string Name => "!клип";
    public string Parameters => "поисковый запрос или пусто";

    public override bool CanExecute(ParsedCommand command)
    {
        // Могут использовать не игнорируемые пользователи в официально подключенных чатах всех сервисов
        return command.UserRole != UserRole.Ignored && CommandPermissionChecker.IsOfficialChannel(command);
    }

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Parameters))
            return await twitchChatService.ClipCreate().ConfigureAwait(false)
                ? "Клип создан."
                : "Не получилось создать клип.";

        var clips = clipsService.GetAllClips().ToList();
        if (!clips.Any()) return "Список клипов пуст.";

        var clipDescriptions = string.Join(Environment.NewLine, clips.Select(c => $"Клип {c.Id}: \"{c.Name}\" - {c.Description}"));
        var gptRequest =
            $"Вот список клипов:{clipDescriptions}\nИскомый запрос: {command.Parameters}\nКакой номер клипа наиболее соответствует запросу? В ответе укажи только цифру номера клипа.";

        // Отправка запроса в Gemini
        var gptResponse = await geminiService.AskGeminiResponseAsync(gptRequest, command.Context.MaxMessageLength).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(gptResponse)) return "Не удалось найти клип.";

        // Попытка найти номер клипа в ответе
        if (!int.TryParse(gptResponse.Trim(), out var clipId)) return "Не удалось найти клип..";

        var clip = clips.FirstOrDefault(c => c.Id == clipId);
        return clip != null ? clip.Url : "Не удалось найти клип...";
    }

    protected override bool HasRequiredParameters(ref ParsedCommand command)
    {
        return true;
    }
}