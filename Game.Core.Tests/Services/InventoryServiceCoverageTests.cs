using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class InventoryServiceCoverageTests
{
    [Fact]
    public void ShouldReturnZero_WhenDistinctSlotsExceededOnAdd()
    {
        var inventory = new Inventory();
        inventory.Add("wood", 1);
        var service = new InventoryService(inventory, maxSlots: 1);

        var added = service.Add("iron", 2);

        added.Should().Be(0);
        service.CountDistinct().Should().Be(1);
        service.CountItem("iron").Should().Be(0);
    }

    [Fact]
    public void ShouldUseStackRulesAndExposeCounts_WhenAddingItems()
    {
        var inventory = new Inventory();
        var service = new InventoryService(inventory, maxSlots: 5);

        var first = service.Add("ammo", 5, maxStack: 6);
        var second = service.Add("ammo", 5, maxStack: 6);

        first.Should().Be(5);
        second.Should().Be(1);
        service.CountItem("ammo").Should().Be(6);
        service.HasItem("ammo", 6).Should().BeTrue();
    }

    [Fact]
    public void ShouldDelegateAndHandleMissingItem_WhenRemovingItems()
    {
        var inventory = new Inventory();
        var service = new InventoryService(inventory, maxSlots: 5);
        service.Add("core", 3);

        var removed = service.Remove("core", 2);
        var missing = service.Remove("none", 1);

        removed.Should().Be(2);
        missing.Should().Be(0);
        service.CountItem("core").Should().Be(1);
    }
}
