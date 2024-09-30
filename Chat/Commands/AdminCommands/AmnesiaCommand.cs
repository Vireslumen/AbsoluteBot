using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;
using AbsoluteBot.Services.NeuralNetworkServices;

namespace AbsoluteBot.Chat.Commands.AdminCommands;

/// <summary>
///     Команда очистки истории сообщений у бота в диалоговом режиме чата с использованием Gemini.
/// </summary>
public class AmnesiaCommand(ChatGeminiService geminiService) : BaseCommand
{
    public override int Priority => 706;
    public override string Description => "удаляет воспоминания чат бота.";
    public override string Name => "!амнезия";

    public override bool CanExecute(ParsedCommand command)
    {
        // Использовать могут администраторы и бот
        return command.UserRole is UserRole.Administrator or UserRole.Bot;
    }

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        if (!await geminiService.ClearPlatformChatHistoryAsync(command.Context.Platform).ConfigureAwait(false))
            return "Не удалось стереть память...";

        return await geminiService.AddUserMessageToChatHistoryOnPlatform("Произошло что-то из-за чего ты решила стереть себе память.", "System",
            command.Context.Platform).ConfigureAwait(false)
            ? "Память успешно стёрта..."
            : "Не удалось вспомнить, что память стёрта.";
    }
}