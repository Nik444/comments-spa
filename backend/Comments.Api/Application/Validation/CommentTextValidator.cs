using System.Text.RegularExpressions;

namespace Comments.Api.Application.Validation;

/// <summary>
/// Проверяет, что в тексте комментария есть только разрешённые теги: &lt;a href="..." title="..."&gt;, &lt;code&gt;, &lt;i&gt;, &lt;strong&gt;,
/// и что они правильно закрыты и вложены. Текст не меняет, только сообщает об ошибках.
/// </summary>
public static class CommentTextValidator
{
    private const int MaxErrors = 10;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    private static readonly HashSet<string> SimpleTags = ["code", "i", "strong"];
    private static readonly HashSet<string> AllowedTags = ["a", "code", "i", "strong"];

    // Одиночный '<' (например, "a < b") тегом не считается, а всё, что похоже на тег, обязано быть разрешённым.
    private static readonly Regex TagRegex = new("<(?=[A-Za-z/!?])[^>]*(?<end>>)?", RegexOptions.CultureInvariant, RegexTimeout);

    // Строгий вид ссылки: href обязателен и идёт первым, title необязателен, только двойные кавычки.
    private static readonly Regex AnchorRegex = new(
        """^a\s+href="(?<href>[^"<>]*)"(\s+title="[^"<>]*")?\z""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeout);

    public static IReadOnlyList<string> Validate(string? text)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(text))
            return errors;

        var openTags = new Stack<string>();

        foreach (Match match in TagRegex.Matches(text))
        {
            if (errors.Count >= MaxErrors)
                break;

            if (!match.Groups["end"].Success)
            {
                errors.Add("Тег не завершён: не найден закрывающий символ '>'.");
                break;
            }

            ReadTag(match.Value[1..^1].Trim(), openTags, errors);
        }

        foreach (var name in openTags.Take(MaxErrors - errors.Count))
            errors.Add($"Тег <{name}> не закрыт.");

        return errors;
    }

    private static void ReadTag(string body, Stack<string> openTags, List<string> errors)
    {
        if (body.StartsWith('/'))
        {
            ReadClosingTag(body[1..].Trim().ToLowerInvariant(), openTags, errors);
            return;
        }

        var lowered = body.ToLowerInvariant();

        if (SimpleTags.Contains(lowered))
        {
            openTags.Push(lowered);
            return;
        }

        var anchor = AnchorRegex.Match(body);

        if (anchor.Success)
        {
            if (!HttpUrl.IsValid(anchor.Groups["href"].Value))
                errors.Add("Ссылка в href должна быть полным адресом http или https.");

            openTags.Push("a");
            return;
        }

        var nameEnd = lowered.IndexOfAny([' ', '\t', '\r', '\n', '/']);
        var name = nameEnd < 0 ? lowered : lowered[..nameEnd];

        if (!AllowedTags.Contains(name))
        {
            errors.Add($"Тег <{name}> не разрешён.");
            return;
        }

        errors.Add(name == "a"
            ? "Ссылка должна иметь вид <a href=\"https://...\" title=\"...\">, title необязателен."
            : $"Тег <{name}> не должен содержать атрибуты или '/'.");

        openTags.Push(name);
    }

    private static void ReadClosingTag(string name, Stack<string> openTags, List<string> errors)
    {
        if (!AllowedTags.Contains(name))
            errors.Add($"Закрывающий тег </{name}> не разрешён.");
        else if (openTags.Count == 0)
            errors.Add($"Лишний закрывающий тег </{name}>: он ничего не закрывает.");
        else if (openTags.Peek() != name)
            errors.Add($"Теги закрыты в неправильном порядке: ожидался </{openTags.Peek()}>, найден </{name}>.");
        else
            openTags.Pop();
    }
}
