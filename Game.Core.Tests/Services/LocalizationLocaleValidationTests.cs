using Game.Core.Services;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class LocalizationLocaleValidationTests
{
    // ACC:T28.8
    [Theory]
    [InlineData("fr-FR")]
    [InlineData("ja-JP")]
    [InlineData("")]
    public void ShouldRejectAndKeepCurrentLocaleUnchanged_WhenRequestLocaleIsUnsupported(string requestedLocale)
    {
        var manager = new LocalizationManager();
        manager.SwitchLocale("zh-CN").Should().BeTrue();
        var localeBeforeRequest = manager.CurrentLocale;

        var accepted = manager.SwitchLocale(requestedLocale);

        accepted.Should().BeFalse("only en-US and zh-CN are allowed in first release");
        manager.CurrentLocale.Should().Be(localeBeforeRequest, "unsupported locale requests must not mutate current locale");
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    public void ShouldAcceptAndApplyLocale_WhenRequestLocaleIsSupported(string requestedLocale)
    {
        var manager = new LocalizationManager();

        var accepted = manager.SwitchLocale(requestedLocale);

        accepted.Should().BeTrue();
        manager.CurrentLocale.Should().Be(requestedLocale);
    }
}
