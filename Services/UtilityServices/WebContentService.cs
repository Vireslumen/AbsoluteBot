using System.Text;
using System.Text.RegularExpressions;
using AbsoluteBot.Helpers;
using HtmlAgilityPack;
using Serilog;

namespace AbsoluteBot.Services.UtilityServices;

/// <summary>
///     Сервис для получения основного текстового содержимого веб-страниц с удалением HTML-кода и других элементов.
/// </summary>
public partial class WebContentService
{
    private const string UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36";
    private const string AcceptLanguageHeader = "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7";
    private const int MaxContentLength = 1000;
    private readonly HttpClient _httpClient;

    public WebContentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentString);
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(AcceptLanguageHeader);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    ///     Получает и извлекает основное текстовое содержимое веб-страниц из списка URL.
    /// </summary>
    /// <param name="urls">Список URL для загрузки контента.</param>
    /// <returns>Список извлечённого текстового содержимого с каждой страницы или <c>null</c> в случае ошибки.</returns>
    public async Task<List<string>?> GetWebContentAsync(List<string> urls)
    {
        var contentList = new List<string>();

        foreach (var url in urls)
            try
            {
                var content = await FetchAndExtractContentAsync(url).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(content)) contentList.Add(content);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при получении содержимого сайта {Url}", url);
            }

        return contentList;
    }

    /// <summary>
    ///     Очищает текст, удаляя HTML-теги, лишние пробелы и ненужные символы.
    /// </summary>
    /// <param name="text">Текст для очистки.</param>
    /// <returns>Очищенный текст без HTML-тегов и лишних пробелов.</returns>
    private static string CleanText(string text)
    {
        text = HtmlTagsRegex().Replace(text, string.Empty);
        text = ExtraSpacesRegex().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex ExtraSpacesRegex();

    /// <summary>
    ///     Асинхронно загружает содержимое веб-страницы по указанному URL и извлекает текст из тегов <c>p</c>, <c>h1</c>,
    ///     <c>h2</c>, <c>h3</c>, <c>article</c>, <c>section</c> и <c>blockquote</c>.
    /// </summary>
    /// <param name="url">URL веб-страницы для извлечения содержимого.</param>
    /// <returns>Извлечённый текст с ограничением в <see cref="MaxContentLength" /> символов.</returns>
    private async Task<string> FetchAndExtractContentAsync(string url)
    {
        var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        // Извлекается текст только из определённых тегов
        var content = new StringBuilder();
        var nodesToExtract =
            doc.DocumentNode.SelectNodes("//p | //h1 | //h2 | //h3 | //article | //section | //blockquote");

        if (nodesToExtract != null)
            foreach (var node in nodesToExtract)
            {
                // Текст очищается от HTML-тегов и лишних пробелов
                var cleanedText = CleanText(node.InnerText);
                if (!string.IsNullOrWhiteSpace(cleanedText)) content.AppendLine(cleanedText);
            }

        // Ограничивается результат до MaxContentLength символов
        var result = content.ToString();
        result = TextProcessingUtils.CutSentence(result, MaxContentLength);
        return result.Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagsRegex();
}