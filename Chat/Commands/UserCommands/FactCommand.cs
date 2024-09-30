using AbsoluteBot.Chat.Context;
using AbsoluteBot.Services;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
///     Команда для получения случайного факта из википедии.
/// </summary>
public class FactCommand(FactService factService) : BaseCommand
{
    public override int Priority => 407;
    public override string Description => "выдаёт какой-то интересный или не очень факт из википедии.";
    public override string Name => "!факт";

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        return await factService.GetFactAsync().ConfigureAwait(false) ?? "Не удалось раздобыть факт.";
    }
}