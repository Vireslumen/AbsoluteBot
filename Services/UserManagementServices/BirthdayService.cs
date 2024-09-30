using System.Text.Json;
using AbsoluteBot.Models;
using AbsoluteBot.Services.NeuralNetworkServices;
using Serilog;

namespace AbsoluteBot.Services.UserManagementServices;

#pragma warning disable IDE0028
/// <summary>
///     Сервис для управления днями рождения пользователей и отправки поздравлений.
/// </summary>
public class BirthdayService(ChatGptService chatGptService) : IAsyncInitializable
{
    private const string FilePath = "user_birthdays.json";
    private const int MaxMessageLength = 250;
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private List<UserBirthdayInfo> _userBirthdayInfos = new();

    public async Task InitializeAsync()
    {
        _userBirthdayInfos = await LoadUserBirthdaysAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Добавляет или обновляет информацию о дне рождения пользователя с заданным именем и датой.
    /// </summary>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="platform">Название платформы.</param>
    /// <param name="birthDateTime">Дата рождения - месяц и день</param>
    /// <returns><c>true</c>, если операция выполнена успешно; иначе <c>false</c>.</returns>
    public async Task<bool> AddOrUpdateUserBirthday(string username, string platform, DateTime birthDateTime)
    {
        try
        {
            var userBirthdayInfo = GetUserBirthdayInfo(username);

            if (userBirthdayInfo == null)
            {
                // Создание новой записи для пользователя
                userBirthdayInfo = CreateNewUserBirthday(username, platform, birthDateTime);
                _userBirthdayInfos.Add(userBirthdayInfo);
            }
            else
            {
                // Обновление существующей информации
                UpdateExistingUserBirthday(userBirthdayInfo, platform, birthDateTime);
            }

            await SaveUserBirthdaysAsync(_userBirthdayInfos).ConfigureAwait(false); // Сохранение изменений
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при добавлении или обновлении дня рождения пользователя.");
            return false;
        }
    }

    /// <summary>
    ///     Асинхронно поздравляет пользователя с днём рождения, если это необходимо.
    /// </summary>
    /// <param name="userInfo">Информация о пользователе.</param>
    /// <param name="platform">Платформа, на которой осуществляется поздравление.</param>
    /// <returns>Сообщение с поздравлением или <c>null</c>, если поздравление не требуется.</returns>
    public async Task<string?> CongratulateUserAsync(UserBirthdayInfo userInfo, string platform)
    {
        var today = DateTime.Today;

        // Проверка, нужно ли уведомлять на данной платформе
        if (!IsNotificationRequired(userInfo, platform, today))
            return null;

        // Генерация поздравительного сообщения
        var message = await GenerateBirthdayMessage(userInfo,
            userInfo.LastCongratulationDate.Any(kv => kv.Value.HasValue && kv.Value.Value.Date == today)).ConfigureAwait(false);

        // Обновление даты последнего поздравления
        await UpdateLastCongratulationDate(userInfo, platform, today).ConfigureAwait(false);

        return message;
    }

    /// <summary>
    ///     Отключает уведомления о дне рождения для указанного пользователя на указанной платформе.
    /// </summary>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="platform">Платформа для отключения уведомлений.</param>
    /// <returns><c>true</c>, если уведомления успешно отключены; иначе <c>false</c>.</returns>
    public async Task<bool> DisableBirthdayNotificationForPlatformAsync(string username, string platform)
    {
        try
        {
            var userBirthdayInfo = GetUserBirthdayInfo(username);
            if (userBirthdayInfo == null) return false;

            if (!userBirthdayInfo.NotifyOnPlatforms.ContainsKey(platform))
                return false;

            userBirthdayInfo.NotifyOnPlatforms[platform] = false;
            await SaveUserBirthdaysAsync(_userBirthdayInfos).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при выключении уведомлений о дне рождения на платформе.");
            return false;
        }
    }

    /// <summary>
    ///     Находит пользователя по никнейму и генерирует поздравление для него для текущей платформы, если его день рождения
    ///     сегодня.
    /// </summary>
    /// <param name="username">Никнейм пользователя.</param>
    /// <param name="platform">Платформа, на которой будет поздравление.</param>
    /// <returns>Текст поздравления или null, если поздравлять не надо.</returns>
    public async Task<string?> FindAndCongratulateUser(string username, string platform)
    {
        var userInfo = GetUserBirthdayInfo(username);
        if (userInfo == null) return null;

        return await CongratulateUserAsync(userInfo, platform).ConfigureAwait(false);
    }

    /// <summary>
    ///     Получает список всех дней рождения пользователей.
    /// </summary>
    /// <returns>Список всех пользователей с информацией о их днях рождения.</returns>
    public List<UserBirthdayInfo> GetAllBirthdays()
    {
        return _userBirthdayInfos;
    }

    /// <summary>
    ///     Получает количество дней до следующего дня рождения на указанной платформе.
    /// </summary>
    /// <param name="platform">Платформа для получения ближайшего дня рождения.</param>
    /// <returns>
    ///     Кортеж, содержащий количество дней до следующего дня рождения и имя пользователя,
    ///     или <c>null</c>, если ближайший день рождения не найден.
    /// </returns>
    public (int daysUntil, string username)? GetDaysUntilNextBirthdayForPlatform(string platform)
    {
        try
        {
            var today = DateTime.Today;
            var upcomingBirthdays = FindNextBirthdayForPlatform(platform, today);
            if (upcomingBirthdays == null) return null;
            var daysUntilNextBirthday = (upcomingBirthdays.NextBirthday - today).Days;
            return (daysUntilNextBirthday, upcomingBirthdays.User.UserName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при получении следующего дня рождения.");
            return null;
        }
    }

    /// <summary>
    ///     Получает количество дней до дня рождения пользователя на указанной платформе.
    /// </summary>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="platform">Название платформы.</param>
    /// <returns>Количество дней до дня рождения или -1, если день рождения не найден.</returns>
    public int GetDaysUntilUserBirthdayForPlatform(string username, string platform)
    {
        try
        {
            var userInfo = GetUserBirthdayInfo(username);
            if (userInfo == null || !userInfo.NotifyOnPlatforms.TryGetValue(platform, out var notify) || !notify)
                return -1;

            var today = DateTime.Today;
            var nextBirthday = userInfo.DateOfBirth.AddYears(today.Year - userInfo.DateOfBirth.Year);

            if (nextBirthday < today) nextBirthday = nextBirthday.AddYears(1);

            return (nextBirthday - today).Days;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка расчёта дней до дня рождения пользователя.");
            return -1;
        }
    }

    /// <summary>
    ///     Получает список списков никнеймов пользователей, у которых день рождения сегодня.
    /// </summary>
    /// <returns>Список никнеймов пользователей с днями рождения, совпадающими с текущей датой.</returns>
    public List<List<string>> GetTodayBirthdaysNicknames()
    {
        var today = DateTime.Today;
        return _userBirthdayInfos.Where(u => u.DateOfBirth.Day == today.Day && u.DateOfBirth.Month == today.Month)
            .Select(u => u.Nicknames)
            .ToList();
    }

    /// <summary>
    ///     Создает новую запись о дне рождения пользователя.
    /// </summary>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="platform">Платформа, на которой будут отправляться уведомления.</param>
    /// <param name="birthDateTime">Дата рождения - месяц и день</param>
    /// <returns>Объект <see cref="UserBirthdayInfo" /> с заполненными данными.</returns>
    private static UserBirthdayInfo CreateNewUserBirthday(string username, string platform, DateTime birthDateTime)
    {
        return new UserBirthdayInfo
        {
            UserName = username,
            Nicknames = new List<string> {username},
            DateOfBirth = birthDateTime,
            NotifyOnPlatforms = new Dictionary<string, bool> {{platform, true}}
        };
    }

    /// <summary>
    ///     Находит ближайший день рождения пользователя на указанной платформе.
    /// </summary>
    /// <param name="platform">Платформа для поиска ближайшего дня рождения.</param>
    /// <param name="today">Текущая дата.</param>
    /// <returns>
    ///     Объект, содержащий информацию о пользователе и его следующем дне рождения, или <c>null</c>, если ближайший
    ///     день рождения не найден.
    /// </returns>
    private dynamic? FindNextBirthdayForPlatform(string platform, DateTime today)
    {
        return _userBirthdayInfos
            .Where(u => u.NotifyOnPlatforms.TryGetValue(platform, out var notify) && notify)
            .Select(u => new
            {
                User = u,
                NextBirthday = u.DateOfBirth.AddYears(today.Year - u.DateOfBirth.Year)
            })
            .Where(u => u.NextBirthday >= today)
            .MinBy(u => u.NextBirthday);
    }

    /// <summary>
    ///     Генерирует поздравительное сообщение для пользователя на основе информации о его дне рождения.
    /// </summary>
    /// <param name="userInfo">Информация о пользователе, которому нужно сгенерировать поздравление.</param>
    /// <param name="alreadyCongratulated">Флаг, указывающий, было ли уже отправлено поздравление сегодня.</param>
    /// <returns>Сгенерированное поздравительное сообщение или <c>null</c>, если сообщение не требуется.</returns>
    private async Task<string?> GenerateBirthdayMessage(UserBirthdayInfo userInfo, bool alreadyCongratulated)
    {
        var birthdayMessage = alreadyCongratulated
            ? $"Ты уже не первый раз за сегодня поздравляешь {userInfo.UserName} с днём рождения."
            : $"Составь теплое поздравление с днем рождения для {userInfo.UserName}, друга в процессе становления! 🥳 Вырази свои теплые пожелания, поделись позитивными надеждами на будущее, добавь немного легкого юмора и создай простое, насыщенное ощущением праздника поздравление. Пусть оно будет дружелюбным и непринужденным, искренним и простым, чтобы подчеркнуть важность этого дня для {userInfo.UserName}, без использования шаблонных фраз или вставок.";

        return await chatGptService.AskChatGptAsync(birthdayMessage, MaxMessageLength).ConfigureAwait(false);
    }

    /// <summary>
    ///     Получает информацию о дне рождения пользователя по его имени.
    /// </summary>
    /// <param name="username">Имя пользователя.</param>
    /// <returns>Информация о дне рождения пользователя или <c>null</c>, если информация не найдена.</returns>
    private UserBirthdayInfo? GetUserBirthdayInfo(string username)
    {
        return _userBirthdayInfos
            .FirstOrDefault(u => u.Nicknames.Any(nickname =>
                nickname.Equals(username, StringComparison.InvariantCultureIgnoreCase)));
    }

    /// <summary>
    ///     Проверяет, нужно ли отправить уведомление о дне рождения на указанной платформе для данного пользователя.
    /// </summary>
    /// <param name="userInfo">Информация о пользователе.</param>
    /// <param name="platform">Платформа для проверки.</param>
    /// <param name="today">Текущая дата.</param>
    /// <returns><c>true</c>, если уведомление требуется; иначе <c>false</c>.</returns>
    private static bool IsNotificationRequired(UserBirthdayInfo userInfo, string platform, DateTime today)
    {
        if (!userInfo.NotifyOnPlatforms.TryGetValue(platform, out var shouldNotify) || !shouldNotify)
            return false;

        if (userInfo.LastCongratulationDate.TryGetValue(platform, out var lastCongratulationDate) &&
            lastCongratulationDate.HasValue &&
            lastCongratulationDate.Value.Year == today.Year)
            return false;

        return userInfo.DateOfBirth.Day == today.Day && userInfo.DateOfBirth.Month == today.Month;
    }

    /// <summary>
    ///     Асинхронно загружает информацию о днях рождения из файла.
    /// </summary>
    /// <returns>Список с информацией о днях рождения пользователей.</returns>
    private static async Task<List<UserBirthdayInfo>> LoadUserBirthdaysAsync()
    {
        await Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(FilePath))
            {
                Log.Warning("Не удалось загрузить список дней рождения пользователей, создание нового.");
                var emptyList = new List<UserBirthdayInfo>();
                var initialBirthdaysJson = JsonSerializer.Serialize(emptyList, JsonOptions);
                await File.WriteAllTextAsync(FilePath, initialBirthdaysJson).ConfigureAwait(false);
                return emptyList;
            }

            var birthdaysJson = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<UserBirthdayInfo>>(birthdaysJson) ?? new List<UserBirthdayInfo>();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Ошибка при загрузке дней рождения из файла.");
            return new List<UserBirthdayInfo>();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    ///     Асинхронно сохраняет информацию о днях рождения в файл.
    /// </summary>
    /// <param name="userBirthdayInfos">Список информации о днях рождения пользователей.</param>
    /// <returns>Задача, представляющая асинхронную операцию сохранения.</returns>
    private static async Task SaveUserBirthdaysAsync(List<UserBirthdayInfo> userBirthdayInfos)
    {
        await Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var birthdaysJson = JsonSerializer.Serialize(userBirthdayInfos, JsonOptions);
            await File.WriteAllTextAsync(FilePath, birthdaysJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при сохранении дней рождения в файл.");
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    ///     Обновляет существующую запись о дне рождения пользователя.
    /// </summary>
    /// <param name="userBirthdayInfo">Информация о дне рождения пользователя для обновления.</param>
    /// <param name="platform">Платформа, на которой будут отправляться уведомления.</param>
    /// <param name="birthDateTime">Дата рождения - месяц и день</param>
    private static void UpdateExistingUserBirthday(UserBirthdayInfo userBirthdayInfo, string platform, DateTime birthDateTime)
    {
        userBirthdayInfo.DateOfBirth = birthDateTime;
        userBirthdayInfo.NotifyOnPlatforms[platform] = true;
    }

    /// <summary>
    ///     Асинхронно обновляет дату последнего поздравления для пользователя на указанной платформе.
    /// </summary>
    /// <param name="userInfo">Информация о пользователе.</param>
    /// <param name="platform">Платформа для обновления даты поздравления.</param>
    /// <param name="today">Текущая дата.</param>
    /// <returns>Задача, представляющая асинхронную операцию обновления.</returns>
    private async Task UpdateLastCongratulationDate(UserBirthdayInfo userInfo, string platform, DateTime today)
    {
        userInfo.LastCongratulationDate[platform] = today;
        await SaveUserBirthdaysAsync(_userBirthdayInfos).ConfigureAwait(false);
    }
}