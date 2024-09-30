using System.Diagnostics;
using System.Reflection;
using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;

namespace AbsoluteBot.Chat.Commands.AdminCommands;

/// <summary>
///     Команда перезагрузки приложения.
/// </summary>
public class RestartCommand : BaseCommand
{
    public override int Priority => -11;
    public override string Name => "!перезагрузка";
    public override string Description => "перезапускает приложение.";

    public override bool CanExecute(ParsedCommand command)
    {
        // Использовать могут администраторы
        return command.UserRole == UserRole.Administrator;
    }

    protected override Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        Task.Run(RestartApplication);
        return Task.FromResult("Перезагрузка приложения...");
    }

    private static void RestartApplication()
    {
        var dllPath = Assembly.GetExecutingAssembly().Location;
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = dllPath,
            UseShellExecute = false
        };

        Process.Start(processStartInfo);
        Environment.Exit(0);
    }
}