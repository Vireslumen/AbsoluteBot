using System.Text.RegularExpressions;

namespace AbsoluteBot.Services.MediaServices;

#pragma warning disable IDE0028
/// <summary>
///     Сервис для поиска изображений в интернете с использованием Google Image Search.
/// </summary>
public partial class ImageSearchService(HttpClient httpClient)
{
    private const string UserAgentHeader =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36";

    private const string AcceptLanguageHeader = "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7";
    private const string PlaceholderImageUrl = "https://i.ytimg.com/vi/KoVjqdETurw/maxresdefault.jpg";
    private const int MaxUrlsForSelection = 15;
    private const int MaxCacheSize = 100;
    protected readonly Random Random = new();
    private readonly HashSet<string> _usedImgUrls = new();
    private readonly List<string> _excludeImageWords = new() {"sun9", "shutterstock", "deposit"};

    /// <summary>
    ///     Выполняет асинхронный поиск изображения на основе переданного текста.
    /// </summary>
    /// <param name="text">Текст для поиска изображения.</param>
    /// <returns>URL найденного изображения или URL изображения с котиком, если поиск не удался.</returns>
    public async Task<string> SearchImageAsync(string text)
    {
        try
        {
            var query = CleanQuery(text);
            var searchUrl = BuildSearchUrl(query);

            var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.Add("User-Agent", UserAgentHeader);
            request.Headers.Add("Accept-Language", AcceptLanguageHeader);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentImagesPage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!contentImagesPage.Contains(".jpg\""))
                return PlaceholderImageUrl;

            var urls = ExtractImageUrls(contentImagesPage);

            //Возвращается случайное изображение из найденных или если ничего не найдено котик
            return urls.Count == 0 ? PlaceholderImageUrl : SelectRandomUrl(urls);
        }
        catch (Exception)
        {
            return PlaceholderImageUrl;
        }
    }

    /// <summary>
    ///     Добавляет URL в кэш, удаляя старые URL, если размер кэша превышает ограничение.
    /// </summary>
    /// <param name="url">URL для добавления в кэш.</param>
    private void AddToCache(string url)
    {
        if (_usedImgUrls.Count >= MaxCacheSize)
            // Удаление старейшего элемента из кэша
            _usedImgUrls.Remove(_usedImgUrls.Last());

        _usedImgUrls.Add(url);
    }

    /// <summary>
    ///     Создаёт URL для поиска изображений на основе запроса.
    /// </summary>
    /// <param name="query">Поисковый запрос.</param>
    /// <returns>URL для поиска изображений.</returns>
    private static string BuildSearchUrl(string query)
    {
        return $"https://www.google.com/search?q={query}&tbm=isch&safe=active";
    }

    /// <summary>
    ///     Очищает поисковый запрос, удаляя лишние пробелы и заменяя их на символ "+".
    /// </summary>
    /// <param name="text">Текст для очистки.</param>
    /// <returns>Очищенный текст.</returns>
    private static string CleanQuery(string text)
    {
        text = text.Trim().Replace(" ", "+");
        return text.ToLower();
    }

    /// <summary>
    ///     Извлекает URL изображений из HTML-ответа на поисковый запрос.
    /// </summary>
    /// <param name="reply">HTML-ответ от поисковой системы.</param>
    /// <returns>Список URL изображений.</returns>
    private List<string> ExtractImageUrls(string reply)
    {
        var matchList = ImageRegex().Matches(reply);
        var urls = matchList.Select(match => match.Value.TrimEnd('"')).ToList();

        // Удаление url изображений с исключающими словами
        urls.RemoveAll(url => _excludeImageWords.Any(url.Contains));

        return urls;
    }

    [GeneratedRegex("http[:a-zA-Z.\\/0-9_-]*jpg\"")]
    private static partial Regex ImageRegex();

    /// <summary>
    ///     Выбирает случайный URL изображения из списка, избегая ранее использованных URL.
    /// </summary>
    /// <param name="urls">Список URL изображений.</param>
    /// <returns>Выбранный случайным образом URL изображения.</returns>
    private string SelectRandomUrl(List<string> urls)
    {
        // Фильтруется список URL, удаляя те, которые уже были использованы
        var availableUrls = urls.Except(_usedImgUrls).Take(MaxUrlsForSelection).ToList();
        string selectedUrl;

        // Если после фильтрации список стал пустым, используется оригинальный список
        if (availableUrls.Count == 0)
        {
            availableUrls = urls;
            selectedUrl = availableUrls[Random.Next(availableUrls.Count)];
        }
        else
        {
            selectedUrl = availableUrls.First();
        }

        AddToCache(selectedUrl);

        return selectedUrl;
    }
}