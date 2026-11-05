using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Comments.Api.Application.Requests;

namespace Comments.Tests.Application.Requests;

public class CreateCommentRequestTests
{
    [Theory]
    [InlineData("Nikolay")]
    [InlineData("user123")]
    [InlineData("A")]
    [InlineData("007")]
    public void UserName_AcceptsLatinLettersAndDigits(string name)
    {
        Assert.True(UserNameRule().IsValid(name));
    }

    [Theory]
    [InlineData("Николай")]
    [InlineData("john smith")]
    [InlineData("john_smith")]
    [InlineData("john-smith")]
    [InlineData("john@")]
    [InlineData(" john")]
    [InlineData("john\n")]
    [InlineData("<script>")]
    [InlineData("Jöhn")]
    public void UserName_RejectsEverythingElse(string name)
    {
        Assert.False(UserNameRule().IsValid(name));
    }

    [Fact]
    public void UserName_RuleHasUnderstandableMessage()
    {
        Assert.Contains("латинские буквы и цифры", UserNameRule().ErrorMessage);
    }

    private static RegularExpressionAttribute UserNameRule() =>
        typeof(CreateCommentRequest)
            .GetConstructors()[0]
            .GetParameters()
            .Single(p => p.Name == nameof(CreateCommentRequest.UserName))
            .GetCustomAttribute<RegularExpressionAttribute>()!;
}
