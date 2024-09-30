using AbsoluteBot.Chat.Context;
using AbsoluteBot.Models;
using AbsoluteBot.Services.UserManagementServices;
using System.Globalization;

namespace AbsoluteBot.Chat.Commands.UserCommands;

/// <summary>
///     Команда добавления своего дня рождения в список для оповещения ботом.
/// </summary>
public class AddBirthdayCommand(BirthdayService birthdayService) : BaseCommand, IParameterized
{
    public override int Priority => 304;
    public override string Description => "добавляет информацию о вашем дне рождения.";
    public override string Name => "!добавитьдр";
    public string Parameters => "день и месяц";

    public override bool CanExecute(ParsedCommand command)
    {
        // Могут использовать не игнорируемые пользователи в официально подключенных чатах всех сервисов
        return command.UserRole != UserRole.Ignored && CommandPermissionChecker.IsOfficialChannel(command);
    }

    protected override async Task<string> ExecuteLogicAsync(ParsedCommand command)
    {
        var context = command.Context;
        var platform = context.Platform;
        var username = context.Username;

        var parameters = command.Parameters.Split(' ');
        if (parameters.Length < 2 ||
            !int.TryParse(parameters[0], out var day) ||
            !int.TryParse(parameters[1], out var month))
            return $"Неверный формат даты. Используйте: {Name} {Parameters}";

        var date = new DateTime(DateTime.MinValue.Year, month, day);
        var formattedDate = date.ToString("d MMMM", new CultureInfo("ru-RU"));
        return await birthdayService.AddOrUpdateUserBirthday(username, platform, date)
            .ConfigureAwait(false)
            ? $"День рождения успешно добавлен: {formattedDate}. Уведомления включены на платформе {platform}."
            : "Не удалось добавить ваш день рождения.";
    }
}