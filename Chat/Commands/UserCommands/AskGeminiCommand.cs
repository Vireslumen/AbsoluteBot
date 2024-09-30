using AbsoluteBot.Chat.Context;
using AbsoluteBot.Services.ChatServices.Interfaces;
using AbsoluteBot.Services.NeuralNetworkServices;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
///     Команда получения ответа на вопрос с помощью Gemini.
/// </summary>
public class AskGeminiCommand(AskGeminiService geminiService) : BaseCommand, IParameterized
{
    public override int Priority => 2;
    public override string Description => "выдаёт ответ на практически любой вопрос с помощью другой нейросети.";
    public override string Name => "!!спросить";
    public string Parameters => "вопрос";

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        string? response;
        if (command.Context.ChatService is IChatImageService chatImageService)
        {
            var image = await chatImageService.GetImageAsBase64Async(command.Parameters, command.Context);
            response = await geminiService.AskGeminiResponseAsync(command.Parameters, command.Context.MaxMessageLength, image).ConfigureAwait(false);
        }
        else
        {
            response = await geminiService.AskGeminiResponseAsync(command.Parameters, command.Context.MaxMessageLength).ConfigureAwait(false);
        }

        return response ?? "Не удалось получить ответ.";
    }
}