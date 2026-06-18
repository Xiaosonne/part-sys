using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using InventorySystem.Infrastructure.Services;

namespace InventorySystem.Tests.Services;

public class StockServiceTests
{
    private readonly Mock<IPartRepository> _partRepoMock;
    private readonly Mock<IStockTransactionRepository> _txRepoMock;
    private readonly StockService _service;

    public StockServiceTests()
    {
        _partRepoMock = new Mock<IPartRepository>();
        _txRepoMock = new Mock<IStockTransactionRepository>();
        _service = new StockService(_partRepoMock.Object, _txRepoMock.Object);
    }

    // InboundAsync

    [Fact]
    public async Task InboundAsync_ShouldIncreaseStockAndCreateTransaction()
    {
        await _service.InboundAsync("part1", 10, "user1", StockSourceType.Purchase, "test note");
        _partRepoMock.Verify(r => r.UpdateQuantitiesAsync("part1", 0, 10,
            It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t =>
            t.PartId == "part1" && t.Type == "INBOUND" && t.Quantity == 10 &&
            t.SourceType == StockSourceType.Purchase && t.Note == "test note")), Times.Once);
    }

    [Fact]
    public async Task InboundAsync_WithNullNote_ShouldStoreEmpty()
    {
        await _service.InboundAsync("part1", 5, "user1", StockSourceType.Manual, null);
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t => t.Note == string.Empty)), Times.Once);
    }

    // OutboundAsync

    [Fact]
    public async Task OutboundAsync_WithSufficientStock_ShouldDeduct()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", 0, -3,
                It.Is<int?>(v => v == 3), It.IsAny<int?>())).ReturnsAsync(true);
        await _service.OutboundAsync("part1", 3, "user1", "proj1", "r1", "Zhang", "test");
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t =>
            t.PartId == "part1" && t.Type == "OUTBOUND" && t.Quantity == 3)), Times.Once);
    }

    [Fact]
    public async Task OutboundAsync_WhenInsufficientStock_ShouldThrow()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", 0, -99,
                It.Is<int?>(v => v == 99), It.IsAny<int?>())).ReturnsAsync(false);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1"))
            .ReturnsAsync(new Part { Id = "part1", Name = "T", TotalQty = 10, LockedQty = 5 });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.OutboundAsync("part1", 99, "user1", null, null, null, null));
        Assert.Contains("不足", ex.Message);
    }

    [Fact]
    public async Task OutboundAsync_WhenPartNotFound_ShouldThrow()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", 0, -1,
                It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(false);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync((Part?)null);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.OutboundAsync("part1", 1, "user1", null, null, null, null));
        Assert.Contains("不存在", ex.Message);
    }

    // OutboundLockedAsync

    [Fact]
    public async Task OutboundLockedAsync_ShouldDeductLockedStock()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", -3, -3,
                It.IsAny<int?>(), It.Is<int?>(v => v == 3))).ReturnsAsync(true);
        await _service.OutboundLockedAsync("part1", 3, "user1", "proj1", null, null, "test",
            StockSourceType.SelectionOutbound);
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t =>
            t.Type == "OUTBOUND" && t.SourceType == StockSourceType.SelectionOutbound)), Times.Once);
    }

    [Fact]
    public async Task OutboundLockedAsync_WhenInsufficientLocked_ShouldThrow()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", -99, -99,
                It.IsAny<int?>(), It.Is<int?>(v => v == 99))).ReturnsAsync(false);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1"))
            .ReturnsAsync(new Part { Id = "part1", Name = "T", TotalQty = 10, LockedQty = 5 });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.OutboundLockedAsync("part1", 99, "user1", null, null, null, null));
        Assert.Contains("不足", ex.Message);
    }

    // LockAsync

    [Fact]
    public async Task LockAsync_WithSufficientAvailable_ShouldLock()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", 5, 0,
                It.Is<int?>(v => v == 5), It.IsAny<int?>())).ReturnsAsync(true);
        await _service.LockAsync("part1", 5, "user1", "proj1", "plan1", "item1", "test",
            StockSourceType.SelectionLock);
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t =>
            t.Type == "LOCK" && t.SourceType == StockSourceType.SelectionLock &&
            t.SelectionPlanId == "plan1" && t.SelectionItemId == "item1")), Times.Once);
    }

    [Fact]
    public async Task LockAsync_WhenInsufficientAvailable_ShouldThrow()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", 99, 0,
                It.Is<int?>(v => v == 99), It.IsAny<int?>())).ReturnsAsync(false);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1"))
            .ReturnsAsync(new Part { Id = "part1", Name = "T", TotalQty = 10, LockedQty = 8 });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.LockAsync("part1", 99, "user1", null, null, null, null));
        Assert.Contains("不足", ex.Message);
    }

    // UnlockAsync

    [Fact]
    public async Task UnlockAsync_WithSufficientLocked_ShouldCarryPlanIds()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", -3, 0,
                It.IsAny<int?>(), It.Is<int?>(v => v == 3))).ReturnsAsync(true);
        await _service.UnlockAsync("part1", 3, "user1", "proj1", "plan1", "item1", "test",
            StockSourceType.SelectionUnlock);
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t =>
            t.Type == "UNLOCK" && t.SourceType == StockSourceType.SelectionUnlock &&
            t.SelectionPlanId == "plan1" && t.SelectionItemId == "item1")), Times.Once);
    }

    [Fact]
    public async Task UnlockAsync_WhenInsufficientLocked_ShouldThrow()
    {
        _partRepoMock.Setup(r => r.UpdateQuantitiesAsync("part1", -99, 0,
                It.IsAny<int?>(), It.Is<int?>(v => v == 99))).ReturnsAsync(false);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1"))
            .ReturnsAsync(new Part { Id = "part1", Name = "T", TotalQty = 10, LockedQty = 2 });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UnlockAsync("part1", 99, "user1", null, null, null, null));
        Assert.Contains("不足", ex.Message);
    }

    // ReturnAsync

    [Fact]
    public async Task ReturnAsync_ShouldIncreaseStockAndCreateTransaction()
    {
        await _service.ReturnAsync("part1", 5, "user1", "proj1", "test");
        _partRepoMock.Verify(r => r.UpdateQuantitiesAsync("part1", 0, 5,
            It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        _txRepoMock.Verify(r => r.CreateAsync(It.Is<StockTransaction>(t =>
            t.Type == "RETURN" && t.Quantity == 5)), Times.Once);
    }
}
