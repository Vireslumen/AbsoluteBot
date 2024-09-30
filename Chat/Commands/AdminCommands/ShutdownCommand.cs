using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;

namespace AbsoluteBot.Chat.Commands.AdminCommands;

/// <summary>
///     Команда выключения приложения.
/// </summary>
public class ShutdownCommand : BaseCommand
{
    public override int Priority => -12;
    public override string Name => "!выключить";
    public override string Description => "выключает приложение.";

    public override bool CanExecute(ParsedCommand command)
    {
        // Использовать могут администраторы
        return command.UserRole == UserRole.Administrator;
    }

    protected override Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        Task.Run(ShutdownApplication);
        return Task.FromResult("Приложение выключается...");
    }

    private static void ShutdownApplication()
    {
        Environment.Exit(0);
    }
}