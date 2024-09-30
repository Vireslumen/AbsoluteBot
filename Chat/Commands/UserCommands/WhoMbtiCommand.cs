using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;
using AbsoluteBot.Services.UserManagementServices;
using AbsoluteBot.Services.UtilityServices;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
///     Команда выдачи персонажа из текущий или выбранной игры для пользователя вызвавшего команду или выбранного им
///     человека.
/// </summary>
public class WhoMbtiCommand(MbtiService mbtiService, ConfigService configService) : BaseCommand, IParameterized
{
    public override int Priority => 302;
    public override string Description =>
        "выдаёт какой ты или выбранный тобой пользователь, персонаж из последней игры на стриме или из указанной в команде.";
    public override string Name => "!whombti";
    public string Parameters => "игра или никнейм или пусто";

    public override bool CanExecute(ParsedCommand command)
    {
        // Могут использовать не игнорируемые пользователи в официально подключенных чатах всех сервисов
        return command.UserRole != UserRole.Ignored && CommandPermissionChecker.IsOfficialChannel(command);
    }

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        var parameters = command.Parameters;
        var gameName = await configService.GetConfigValueAsync<string>("LastGameName").ConfigureAwait(false);
        var username = command.Context.Username;
        var userMbti = mbtiService.GetMbtiForUser(username);

        var targetGame = gameName;
        var targetUser = username;

        // Если параметр передан
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            var userMbtiFromParam = mbtiService.GetMbtiForUser(parameters);
            if (userMbtiFromParam != null)
            {
                userMbti = userMbtiFromParam;
                targetUser = parameters;
            }
            else
            {
                targetGame = parameters;
            }
        }

        if (string.IsNullOrWhiteSpace(targetGame)) return "Не удалось найти игру.";

        // Проверка MBTI пользователя
        if (string.IsNullOrWhiteSpace(userMbti)) return "Ваш MBTI не найден. Пожалуйста, сначала установите его с помощью команды !mbti *MBTI*.";

        // Поиск персонажа по mbti
        var character = await mbtiService.GetCharacterByMbtiAsync(targetGame, userMbti).ConfigureAwait(false);
        if (character == null) return "Не удалось найти персонажа.";

        return targetUser == username
            ? $"Вы соответствуете персонажу {character} из {targetGame}."
            : $"{targetUser} соответствует персонажу {character} из {targetGame}.";
    }

    protected override bool HasRequiredParameters(ref ParsedCommand command)
    {
        return true;
    }
}