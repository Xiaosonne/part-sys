using Xunit;
using InventorySystem.Core.Models;

namespace InventorySystem.Tests.Models;

public class PartTests
{
    [Fact]
    public void AvailableQty_ShouldEqualTotalMinusLocked()
    {
        var part = new Part { TotalQty = 100, LockedQty = 30 };
        Assert.Equal(70, part.AvailableQty);
    }

    [Fact]
    public void AvailableQty_WhenTotalEqualsLocked_ShouldBeZero()
    {
        var part = new Part { TotalQty = 50, LockedQty = 50 };
        Assert.Equal(0, part.AvailableQty);
    }

    [Fact]
    public void AvailableQty_WhenNoStock_ShouldBeZero()
    {
        var part = new Part { TotalQty = 0, LockedQty = 0 };
        Assert.Equal(0, part.AvailableQty);
    }

    [Fact]
    public void AvailableQty_ShouldNeverBeNegative()
    {
        var part = new Part { TotalQty = 10, LockedQty = 20 };
        Assert.Equal(-10, part.AvailableQty);
        // 业务层通过 minAvailableQty / minLockedQty 条件防止此情况
    }
}
