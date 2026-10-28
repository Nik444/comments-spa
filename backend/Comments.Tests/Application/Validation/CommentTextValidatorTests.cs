using Comments.Api.Application.Validation;

namespace Comments.Tests.Application.Validation;

public class CommentTextValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Просто текст без тегов")]
    [InlineData("<i>курсив</i>")]
    [InlineData("<strong>жирный</strong>")]
    [InlineData("<code>var x = 1;</code>")]
    [InlineData("<a href=\"https://example.com\">ссылка</a>")]
    [InlineData("<a href=\"http://example.com\" title=\"Подсказка\">ссылка</a>")]
    [InlineData("<A HREF=\"https://example.com\"><I>регистр</I></A>")]
    [InlineData("<strong>жирный <i>и курсив</i></strong>")]
    [InlineData("если 2 < 3 и 5 > 4, то всё хорошо")]
    public void Validate_ReturnsNoErrors_ForAllowedText(string text)
    {
        Assert.Empty(CommentTextValidator.Validate(text));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<b>жирный</b>")]
    [InlineData("<!-- комментарий -->")]
    [InlineData("<?xml version=\"1.0\"?>")]
    public void Validate_ReturnsError_ForForbiddenTags(string text)
    {
        Assert.NotEmpty(CommentTextValidator.Validate(text));
    }

    [Theory]
    [InlineData("<i>не закрыт")]
    [InlineData("закрыт без открытия</i>")]
    [InlineData("<i><strong>неверный порядок</i></strong>")]
    [InlineData("<i>текст</strong>")]
    [InlineData("<a href=\"https://example.com\">ссылка")]
    [InlineData("<i")]
    public void Validate_ReturnsError_ForBrokenStructure(string text)
    {
        Assert.NotEmpty(CommentTextValidator.Validate(text));
    }

    [Theory]
    [InlineData("<a>без href</a>")]
    [InlineData("<a href=\"javascript:alert(1)\">xss</a>")]
    [InlineData("<a href=\"JaVaScRiPt:alert(1)\">xss</a>")]
    [InlineData("<a href=\"data:text/html;base64,PHNjcmlwdD4=\">xss</a>")]
    [InlineData("<a href=\"&#106;avascript:alert(1)\">xss</a>")]
    [InlineData("<a href=\"/относительная\">ссылка</a>")]
    [InlineData("<a href=\"https://example.com\" onclick=\"alert(1)\">xss</a>")]
    [InlineData("<a href=\"https://example.com\" href=\"https://example.org\">дубль</a>")]
    [InlineData("<a href=https://example.com>без кавычек</a>")]
    [InlineData("<a href>без значения</a>")]
    [InlineData("<a href=\"https://example.com\"title=\"слитно\">ссылка</a>")]
    [InlineData("<a href=\"https://example.com\"/>самозакрытый</a>")]
    [InlineData("<a href='https://example.com'>одинарные кавычки</a>")]
    [InlineData("<a title=\"порядок\" href=\"https://example.com\">title раньше href</a>")]
    [InlineData("<a href=\"https://example.com\" title=\"с > внутри\">ссылка</a>")]
    public void Validate_ReturnsError_ForUnsupportedAnchors(string text)
    {
        Assert.NotEmpty(CommentTextValidator.Validate(text));
    }

    [Theory]
    [InlineData("<i class=\"x\">атрибут</i>")]
    [InlineData("<strong onclick=\"alert(1)\">атрибут</strong>")]
    [InlineData("<i/>")]
    public void Validate_ReturnsError_ForAttributesOnSimpleTags(string text)
    {
        Assert.NotEmpty(CommentTextValidator.Validate(text));
    }

    [Fact]
    public void Validate_ReportsWhichTagIsNotClosed()
    {
        var errors = CommentTextValidator.Validate("<i>текст");

        Assert.Equal(new[] { "Тег <i> не закрыт." }, errors);
    }

    [Fact]
    public void Validate_ReportsUnclosedTagsFromInnermost()
    {
        var errors = CommentTextValidator.Validate("<strong><i>текст");

        Assert.Equal(new[] { "Тег <i> не закрыт.", "Тег <strong> не закрыт." }, errors);
    }

    [Fact]
    public void Validate_LimitsNumberOfErrors()
    {
        var errors = CommentTextValidator.Validate(string.Concat(Enumerable.Repeat("<script>", 100)));

        Assert.Equal(10, errors.Count);
    }

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com/path?q=1", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HttpUrl_AcceptsOnlyAbsoluteHttpAndHttps(string? value, bool expected)
    {
        Assert.Equal(expected, HttpUrl.IsValid(value));
    }
}
