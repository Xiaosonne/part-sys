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
            _planRepoMock.Object,
            _purchaseTaskRepoMock.Object,
            _partRepoMock.Object,
            _stockServiceMock.Object,
            _loggerMock.Object);
    }

    private SelectionPlan CreateTestPlan(string id = "plan1", SelectionPlanStatus status = SelectionPlanStatus.Draft)
    {
        return new SelectionPlan
        {
            Id = id,
            Name = "Test Plan",
            ProjectId = "proj1",
            Status = status,
            Items = new List<SelectionItem>()
        };
    }

    private SelectionItem CreateTestItem(string id = "item1", string partId = "part1")
    {
        return new SelectionItem
        {
            Id = id,
            SelectedPartId = partId,
            PartName = "Test Part",
            Category = "Category1",
            RequiredQty = 10,
            LockedQty = 0,
            OutboundQty = 0,
            PendingQty = 0
        };
    }

    private Part CreateTestPart(string id = "part1", int availableQty = 20)
    {
        return new Part
        {
            Id = id,
            Name = "Test Part",
            Category = "Category1",
            AvailableQty = availableQty,
            TotalQty = 20,
            LockedQty = 0
        };
    }

    [Fact]
    public async Task SubmitAsync_WhenPlanNotFound_ThrowsException()
    {
        // Arrange
        _planRepoMock.Setup(r => r.GetByIdAsync("nonexistent"))
            .ReturnsAsync((SelectionPlan?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitAsync("nonexistent", "user1"));
    }

    [Fact]
    public async Task SubmitAsync_WhenPlanNotDraft_ThrowsException()
    {
        // Arrange
        var plan = CreateTestPlan(status: SelectionPlanStatus.Submitted);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitAsync("plan1", "user1"));
    }

    [Fact]
    public async Task SubmitAsync_WhenItemHasNoSelectedPart_ThrowsException()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Items.Add(new SelectionItem
        {
            Id = "item1",
            SelectedPartId = "", // 未选择配件
            PartName = "Test Part",
            RequiredQty = 10
        });
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitAsync("plan1", "user1"));
        Assert.Contains("配件未选择", ex.Message);
    }

    [Fact]
    public async Task SubmitAsync_WhenPartNotFound_ThrowsException()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "nonexistent"));
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("nonexistent"))
            .ReturnsAsync((Part?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubmitAsync("plan1", "user1"));
    }

    [Fact]
    public async Task SubmitAsync_WithSufficientStock_FullyLocks()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "part1"));
        var part = CreateTestPart(id: "part1", availableQty: 20);

        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(part);
        _stockServiceMock.Setup(s => s.LockAsync("part1", 10, "user1", "proj1", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitAsync("plan1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.LockedItems);
        Assert.Equal(10, result.LockedItems[0].LockedQty);
        Assert.Equal(0, result.LockedItems[0].PendingQty);
        Assert.Empty(result.CreatedPurchaseTasks);
        _stockServiceMock.Verify(s => s.LockAsync("part1", 10, "user1", "proj1", "plan1", It.IsAny<string?>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithInsufficientStock_PartiallyLocksAndCreatesPurchaseTask()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "part1"));
        var part = CreateTestPart(id: "part1", availableQty: 5); // 只有5个，但需要10个

        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(part);
        _stockServiceMock.Setup(s => s.LockAsync("part1", 5, "user1", "proj1", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _purchaseTaskRepoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseTask>()))
            .Callback<PurchaseTask>(t => t.Id = "task1")
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitAsync("plan1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.LockedItems);
        Assert.Equal(5, result.LockedItems[0].LockedQty);
        Assert.Equal(5, result.LockedItems[0].PendingQty);
        Assert.Single(result.CreatedPurchaseTasks);
        Assert.Equal(5, result.CreatedPurchaseTasks[0].RequiredQty);
        Assert.Equal(PurchaseTaskStatus.Pending, result.CreatedPurchaseTasks[0].Status);

        _stockServiceMock.Verify(s => s.LockAsync("part1", 5, "user1", "proj1", "plan1", It.IsAny<string?>(), It.IsAny<string>()), Times.Once);
        _purchaseTaskRepoMock.Verify(r => r.CreateAsync(It.IsAny<PurchaseTask>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithZeroStock_CreatesFullPurchaseTask()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem(partId: "part1"));
        var part = CreateTestPart(id: "part1", availableQty: 0);

        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(part);
        _purchaseTaskRepoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseTask>()))
            .Callback<PurchaseTask>(t => t.Id = "task1")
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitAsync("plan1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.LockedItems);
        Assert.Equal(0, result.LockedItems[0].LockedQty);
        Assert.Equal(10, result.LockedItems[0].PendingQty);
        Assert.Single(result.CreatedPurchaseTasks);
        Assert.Equal(10, result.CreatedPurchaseTasks[0].RequiredQty);

        // 不应调用 LockAsync，因为没有可用库存
        _stockServiceMock.Verify(s => s.LockAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_UpdatesPlanStatusToSubmitted()
    {
        // Arrange
        var plan = CreateTestPlan(status: SelectionPlanStatus.Draft);
        plan.Items.Add(CreateTestItem());
        var part = CreateTestPart(availableQty: 20);

        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _partRepoMock.Setup(r => r.GetByIdAsync("part1")).ReturnsAsync(part);
        _stockServiceMock.Setup(s => s.LockAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SubmitAsync("plan1", "user1");

        // Assert
        _planRepoMock.Verify(r => r.UpdateAsync("plan1", It.Is<SelectionPlan>(
            p => p.Status == SelectionPlanStatus.Submitted && p.SubmittedAt != null)), Times.Once);
    }

    [Fact]
    public async Task OutboundAsync_WhenPlanNotFound_ThrowsException()
    {
        // Arrange
        _planRepoMock.Setup(r => r.GetByIdAsync("nonexistent"))
            .ReturnsAsync((SelectionPlan?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.OutboundAsync("nonexistent", "item1", 5, "user1", "proj1", null, null));
    }

    [Fact]
    public async Task OutboundAsync_WhenItemNotFound_ThrowsException()
    {
        // Arrange
        var plan = CreateTestPlan();
        plan.Items.Add(CreateTestItem());
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.OutboundAsync("plan1", "nonexistent", 5, "user1", "proj1", null, null));
    }

    [Fact]
    public async Task OutboundAsync_WhenQtyExceedsLocked_ThrowsException()
    {
        // Arrange
        var plan = CreateTestPlan();
        var item = CreateTestItem();
        item.LockedQty = 5;
        plan.Items.Add(item);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.OutboundAsync("plan1", "item1", 10, "user1", "proj1", null, null));
        Assert.Contains("超过已锁定数量", ex.Message);
    }

    [Fact]
    public async Task OutboundAsync_ValidRequest_DeductsLockedAndAddsOutbound()
    {
        // Arrange
        var plan = CreateTestPlan();
        var item = CreateTestItem();
        item.LockedQty = 10;
        plan.Items.Add(item);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _stockServiceMock.Setup(s => s.OutboundAsync("part1", 5, "user1", "proj1", null, null, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.OutboundAsync("plan1", "item1", 5, "user1", "proj1", null, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.OutboundQty);
        Assert.Equal(5, result.RemainingLockedQty);

        _stockServiceMock.Verify(s => s.OutboundAsync("part1", 5, "user1", "proj1", null, null, It.IsAny<string>()), Times.Once);
        _planRepoMock.Verify(r => r.UpdateAsync("plan1", It.Is<SelectionPlan>(p => p.Items[0].LockedQty == 5 && p.Items[0].OutboundQty == 5)), Times.Once);
    }

    [Fact]
    public async Task OutboundAsync_WhenAllItemsCompleted_SetsPlanCompleted()
    {
        // Arrange
        var plan = CreateTestPlan();
        var item1 = CreateTestItem("item1", "part1");
        item1.LockedQty = 5;
        item1.OutboundQty = 0;
        item1.RequiredQty = 5;
        var item2 = CreateTestItem("item2", "part2");
        item2.LockedQty = 5;
        item2.OutboundQty = 5; // item2 已完成
        item2.RequiredQty = 5;
        plan.Items.Add(item1);
        plan.Items.Add(item2);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _stockServiceMock.Setup(s => s.OutboundAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.OutboundAsync("plan1", "item1", 5, "user1", "proj1", null, null);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsItemCompleted);
        Assert.True(result.IsPlanCompleted); // item2 已完成，item1 现在也完成，所以 plan 是 Completed

        _planRepoMock.Verify(r => r.UpdateAsync("plan1", It.Is<SelectionPlan>(
            p => p.Status == SelectionPlanStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenPlanNotSubmitted_ThrowsException()
    {
        // Arrange
        var plan = CreateTestPlan(status: SelectionPlanStatus.Draft);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelAsync("plan1", "user1"));
    }

    [Fact]
    public async Task CancelAsync_WithLockedItems_UnlocksAll()
    {
        // Arrange
        var plan = CreateTestPlan(status: SelectionPlanStatus.Submitted);
        var item1 = CreateTestItem("item1", "part1");
        item1.LockedQty = 10;
        var item2 = CreateTestItem("item2", "part2");
        item2.LockedQty = 5;
        plan.Items.Add(item1);
        plan.Items.Add(item2);
        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _purchaseTaskRepoMock.Setup(r => r.GetBySelectionPlanIdAsync("plan1"))
            .ReturnsAsync(new List<PurchaseTask>());
        _stockServiceMock.Setup(s => s.UnlockAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CancelAsync("plan1", "user1");

        // Assert
        _stockServiceMock.Verify(s => s.UnlockAsync("part1", 10, "user1", "proj1", It.IsAny<string>()), Times.Once);
        _stockServiceMock.Verify(s => s.UnlockAsync("part2", 5, "user1", "proj1", It.IsAny<string>()), Times.Once);
        _planRepoMock.Verify(r => r.UpdateAsync("plan1", It.Is<SelectionPlan>(
            p => p.Status == SelectionPlanStatus.Cancelled && p.Items.All(i => i.LockedQty == 0))), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WithPendingPurchaseTasks_CancelsThem()
    {
        // Arrange
        var plan = CreateTestPlan(status: SelectionPlanStatus.Submitted);
        var item = CreateTestItem();
        item.LockedQty = 5;
        item.PurchaseTaskId = "task1";
        plan.Items.Add(item);

        var purchaseTask = new PurchaseTask
        {
            Id = "task1",
            Status = PurchaseTaskStatus.Pending
        };

        _planRepoMock.Setup(r => r.GetByIdAsync("plan1")).ReturnsAsync(plan);
        _purchaseTaskRepoMock.Setup(r => r.GetBySelectionPlanIdAsync("plan1"))
            .ReturnsAsync(new List<PurchaseTask> { purchaseTask });
        _stockServiceMock.Setup(s => s.UnlockAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _purchaseTaskRepoMock.Setup(r => r.UpdateAsync("task1", It.IsAny<PurchaseTask>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CancelAsync("plan1", "user1");

        // Assert
        _purchaseTaskRepoMock.Verify(r => r.UpdateAsync("task1", It.Is<PurchaseTask>(
            t => t.Status == PurchaseTaskStatus.Cancelled)), Times.Once);
    }
}
