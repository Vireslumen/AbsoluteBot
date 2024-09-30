using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;
using AbsoluteBot.Services.ChatServices.TwitchChat;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
///     Команда для создания клипа на Twitch. Клип создаётся без названия длинной в 30 секунд.
/// </summary>
public class ClipCommand(TwitchChatService twitchChatService) : BaseCommand
{
    public override int Priority => 10;
    public override string Description => "делает клип со стрима на twitch.";
    public override string Name => "!клип";

    public override bool CanExecute(ParsedCommand command)
    {
        // Могут использовать не игнорируемые пользователи в официально подключенных чатах всех сервисов
        return command.UserRole != UserRole.Ignored && CommandPermissionChecker.IsOfficialChannel(command);
    }

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        return await twitchChatService.ClipCreate().ConfigureAwait(false) 
            ? "Клип создан." 
            : "Не получилось создать клип.";
    }
}