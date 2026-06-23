using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using InventorySystem.Infrastructure.Services;

namespace InventorySystem.Tests.Services;

public class SelectionServiceTests
{
    private readonly Mock<ISelectionPlanRepository> _planRepoMock;
    private readonly Mock<IPurchaseTaskRepository> _purchaseTaskRepoMock;
    private readonly Mock<IPartRepository> _partRepoMock;
    private readonly Mock<IStockService> _stockServiceMock;
    private readonly Mock<ILogger<SelectionService>> _loggerMock;
    private readonly SelectionService _service;

    public SelectionServiceTests()
    {
        _planRepoMock = new Mock<ISelectionPlanRepository>();
        _purchaseTaskRepoMock = new Mock<IPurchaseTaskRepository>();
        _partRepoMock = new Mock<IPartRepository>();
        _stockServiceMock = new Mock<IStockService>();
        _loggerMock = new Mock<ILogger<SelectionService>>();
        _service = new SelectionService(
            _planRepoMock.Object, _purchaseTaskRepoMock.Object,
            _partRepoMock.Object, _stockServiceMock.Object, _loggerMock.Object);
    }

    private SelectionPlan CreateTestPlan(string id = "plan1", SelectionPlanStatus status = SelectionPlanStatus.Draft)
        => new() { Id = id, Name = "Test Plan", ProjectId = "proj1", Status = status, Items = new() };

    private SelectionItem CreateTestItem(string id = "item1", string partId = "part1")
        => new() { Id = id, SelectedPartId = partId, PartName = "Test Part", Category = "Category1", RequiredQty = 10 };

    private Part CreateTestPart(string id = "part1", int totalQty = 20)
        => new() { Id = id, Name = "Test Part", Category = "Category1", TotalQty = totalQty, LockedQty = 0 };

    [Fact]
    public async Task SubmitAsync_WhenPlanNotFound_ThrowsException()
    {
        _planRepoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((SelectionPlan?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync("nonexistent", "user1"));
    }

    [Fact]
    public async Task SubmitAsync_WhenPlanNotDraft_ThrowsException()
    {
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1"))
            .ReturnsAsync(CreateTestPlan(status: SelectionPlanStatus.Submitted));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync("plan1", "user1"));
    }

    [Fact]
    public async Task SubmitAsync_WhenItemHasNoSelectedPart_ThrowsException()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(new SelectionItem { Id = "item1", SelectedPartId = "", PartName = "T", RequiredQty = 10 });
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync("plan1", "user1"));
        Assert.Contains("未选择", ex.Message);
    }

    [Fact]
    public async Task SubmitAsync_WhenPartNotFound_ThrowsException()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "nonexistent"));
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((Part?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync("plan1", "user1"));
    }

    [Fact]
    public async Task SubmitAsync_WithSufficientStock_FullyLocks()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "part1"));
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(CreateTestPart(totalQty: 20));
        _stockServiceMock.Setup(s => s.LockAsync("part1", 10, "user1", "proj1",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask);
        var result = await _service.SubmitAsync("plan1", "user1");
        Assert.True(result.Success);
        Assert.Single(result.LockedItems);
        Assert.Equal(10, result.LockedItems[0].LockedQty);
        Assert.Equal(0, result.LockedItems[0].PendingQty);
        Assert.Empty(result.CreatedPurchaseTasks);
        _stockServiceMock.Verify(s => s.LockAsync("part1", 10, "user1", "proj1", "plan1",
            It.IsAny<string?>(), It.IsAny<string>(), StockSourceType.SelectionLock), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithInsufficientStock_PartiallyLocks()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "part1"));
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(CreateTestPart(totalQty: 5));
        _stockServiceMock.Setup(s => s.LockAsync("part1", 5, "user1", "proj1",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask);
        _purchaseTaskRepoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseTask>()))
            .Callback<PurchaseTask>(t => t.Id = "task1").Returns(Task.CompletedTask);
        var result = await _service.SubmitAsync("plan1", "user1");
        Assert.True(result.Success);
        Assert.Equal(5, result.LockedItems[0].LockedQty);
        Assert.Equal(5, result.LockedItems[0].PendingQty);
        Assert.Single(result.CreatedPurchaseTasks);
    }

    [Fact]
    public async Task SubmitAsync_WithZeroStock_CreatesFullPurchaseTask()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "part1"));
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(CreateTestPart(totalQty: 0));
        _purchaseTaskRepoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseTask>()))
            .Callback<PurchaseTask>(t => t.Id = "task1").Returns(Task.CompletedTask);
        var result = await _service.SubmitAsync("plan1", "user1");
        Assert.True(result.Success);
        Assert.Equal(0, result.LockedItems[0].LockedQty);
        Assert.Equal(10, result.LockedItems[0].PendingQty);
        Assert.Single(result.CreatedPurchaseTasks);
    }

    [Fact]
    public async Task SubmitAsync_UpdatesPlanStatusToSubmitted()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem());
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(CreateTestPart(totalQty: 20));
        _stockServiceMock.Setup(s => s.LockAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask);
        await _service.SubmitAsync("plan1", "user1");
        _planRepoMock.Verify(r => r.UpdateAsync("plan1", It.Is<SelectionPlan>(
            p => p.Status == SelectionPlanStatus.Submitted && p.SubmittedAt != null)), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WhenLockFails_RollsBackPreviousLocks()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem("item1", "part1"));
        plan.Items.Add(CreateTestItem("item2", "part2"));
        plan.Items.Add(CreateTestItem("item3", "part3"));
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(CreateTestPart(totalQty: 20));
        _partRepoMock.Setup(r => r.GetByIdAsync("part2")).ReturnsAsync(CreateTestPart(totalQty: 20));
        _partRepoMock.Setup(r => r.GetByIdAsync("part3")).ReturnsAsync(CreateTestPart(totalQty: 20));
        _stockServiceMock.SetupSequence(s => s.LockAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new InvalidOperationException("concurrency"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync("plan1", "user1"));
        _stockServiceMock.Verify(s => s.UnlockAsync("part1", It.IsAny<int>(), "user1", "proj1",
            "plan1", It.IsAny<string?>(), It.IsAny<string>(), StockSourceType.SelectionUnlock), Times.Once);
        _stockServiceMock.Verify(s => s.UnlockAsync("part2", It.IsAny<int>(), "user1", "proj1",
            "plan1", It.IsAny<string?>(), It.IsAny<string>(), StockSourceType.SelectionUnlock), Times.Once);
        _planRepoMock.Verify(r => r.UpdateAsync("plan1", It.Is<SelectionPlan>(
            p => p.Status == SelectionPlanStatus.Submitted)), Times.Never);
    }

    [Fact]
    public async Task OutboundAsync_WhenPlanNotFound_ThrowsException()
    {
        _planRepoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((SelectionPlan?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.OutboundAsync("nonexistent", "item1", 5, "user1", "proj1", null, null));
    }

    [Fact]
    public async Task OutboundAsync_WhenItemNotFound_ThrowsException()
    {
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem());
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.OutboundAsync("plan1", "nonexistent", 5, "user1", "proj1", null, null));
    }

    [Fact]
    public async Task OutboundAsync_WhenQtyExceedsLocked_ThrowsException()
    {
        var plan = CreateTestPlan();
        var item = CreateTestItem();
        item.LockedQty = 5;
        plan.Items.Add(item);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.OutboundAsync("plan1", "item1", 10, "user1", "proj1", null, null));
        Assert.Contains("超过", ex.Message);
    }

    [Fact]
    public async Task OutboundAsync_ValidRequest_DeductsLockedAndAddsOutbound()
    {
        var plan = CreateTestPlan();
        var item = CreateTestItem();
        item.LockedQty = 10;
        plan.Items.Add(item);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _stockServiceMock.Setup(s => s.OutboundLockedAsync("part1", 5, "user1", "proj1",
            null, null, It.IsAny<string>(), It.IsAny<StockSourceType>())).Returns(Task.CompletedTask);
        var result = await _service.OutboundAsync("plan1", "item1", 5, "user1", "proj1", null, null);
        Assert.True(result.Success);
        Assert.Equal(5, result.OutboundQty);
        Assert.Equal(5, result.RemainingLockedQty);
    }

    [Fact]
    public async Task OutboundAsync_WhenAllItemsCompleted_SetsPlanCompleted()
    {
        var plan = CreateTestPlan();
        var i1 = CreateTestItem("i1", "p1"); i1.LockedQty = 5; i1.RequiredQty = 5;
        var i2 = CreateTestItem("i2", "p2"); i2.LockedQty = 5; i2.OutboundQty = 5; i2.RequiredQty = 5;
        plan.Items.Add(i1); plan.Items.Add(i2);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _stockServiceMock.Setup(s => s.OutboundLockedAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask);
        var result = await _service.OutboundAsync("plan1", "i1", 5, "user1", "proj1", null, null);
        Assert.True(result.IsItemCompleted);
        Assert.True(result.IsPlanCompleted);
    }

    [Fact]
    public async Task CancelAsync_WhenPlanNotSubmitted_ThrowsException()
    {
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1"))
            .ReturnsAsync(CreateTestPlan(status: SelectionPlanStatus.Draft));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelAsync("plan1", "user1"));
    }

    [Fact]
    public async Task CancelAsync_WithLockedItems_UnlocksAllWithPlanIds()
    {
        var plan = CreateTestPlan(status: SelectionPlanStatus.Submitted);
        var i1 = CreateTestItem("i1", "p1"); i1.LockedQty = 10;
        var i2 = CreateTestItem("i2", "p2"); i2.LockedQty = 5;
        plan.Items.Add(i1); plan.Items.Add(i2);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _purchaseTaskRepoMock.Setup(r => r.GetBySelectionPlanIdAsync("plan1"))
            .ReturnsAsync(new List<PurchaseTask>());
        _stockServiceMock.Setup(s => s.UnlockAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask);
        await _service.CancelAsync("plan1", "user1");
        _stockServiceMock.Verify(s => s.UnlockAsync("p1", 10, "user1", "proj1",
            "plan1", "i1", It.IsAny<string>(), StockSourceType.SelectionUnlock), Times.Once);
        _stockServiceMock.Verify(s => s.UnlockAsync("p2", 5, "user1", "proj1",
            "plan1", "i2", It.IsAny<string>(), StockSourceType.SelectionUnlock), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WithPendingPurchaseTasks_CancelsThem()
    {
        var plan = CreateTestPlan(status: SelectionPlanStatus.Submitted);
        var item = CreateTestItem(); item.LockedQty = 5; item.PurchaseTaskId = "task1";
        plan.Items.Add(item);
        var pt = new PurchaseTask { Id = "task1", Status = PurchaseTaskStatus.Pending };
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _purchaseTaskRepoMock.Setup(r => r.GetBySelectionPlanIdAsync("plan1"))
            .ReturnsAsync(new List<PurchaseTask> { pt });
        _stockServiceMock.Setup(s => s.UnlockAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<StockSourceType>()))
            .Returns(Task.CompletedTask);
        _purchaseTaskRepoMock.Setup(r => r.UpdateAsync("task1", It.IsAny<PurchaseTask>()))
            .Returns(Task.CompletedTask);
        await _service.CancelAsync("plan1", "user1");
        _purchaseTaskRepoMock.Verify(r => r.UpdateAsync("task1", It.Is<PurchaseTask>(
            t => t.Status == PurchaseTaskStatus.Cancelled)), Times.Once);
    }
}
