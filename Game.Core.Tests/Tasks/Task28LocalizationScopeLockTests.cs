using Game.Core.Services;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task28LocalizationScopeLockTests
{
    // ACC:T28.1
    [Fact]
    public void ShouldRejectSwitch_WhenLocaleOutsideZhCnAndEnUs()
    {
        var manager = new LocalizationManager();
        manager.SwitchLocale("en-US").Should().BeTrue();
        var localeBeforeRequest = manager.CurrentLocale;

        var accepted = manager.SwitchLocale("fr-FR");

        accepted.Should().BeFalse("first release scope must lock locales to zh-CN and en-US only");
        manager.CurrentLocale.Should().Be(localeBeforeRequest);
    }

    // ACC:T28.1
    [Fact]
    public void ShouldAllowSwitch_WhenLocaleWithinLockedScope()
    {
        var manager = new LocalizationManager();

        var enAccepted = manager.SwitchLocale("en-US");
        var zhAccepted = manager.SwitchLocale("zh-CN");

        enAccepted.Should().BeTrue();
        zhAccepted.Should().BeTrue();
        manager.CurrentLocale.Should().Be("zh-CN");
    }
}
