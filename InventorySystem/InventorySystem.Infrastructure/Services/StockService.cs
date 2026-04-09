using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;

namespace InventorySystem.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly IPartRepository _partRepo;
    private readonly IStockTransactionRepository _txRepo;

    public StockService(IPartRepository partRepo, IStockTransactionRepository txRepo)
    {
        _partRepo = partRepo;
        _txRepo = txRepo;
    }

    public async Task InboundAsync(string partId, int qty, string operatorId, StockSourceType sourceType, string? note)
    {
        await _partRepo.UpdateQuantitiesAsync(partId, qty, 0, qty);
        await _txRepo.CreateAsync(new StockTransaction
        {
            PartId = partId, Type = "INBOUND", Quantity = qty,
            OperatorId = operatorId, SourceType = sourceType, Note = note ?? string.Empty
        });
    }

    public async Task OutboundAsync(string partId, int qty, string operatorId, string? projectId, string? recipientId, string? recipientName, string? note)
    {
        var part = await _partRepo.GetByIdAsync(partId)
            ?? throw new InvalidOperationException("配件不存在");
        if (part.AvailableQty < qty)
            throw new InvalidOperationException($"可用库存不足，当前可用：{part.AvailableQty}");

        await _partRepo.UpdateQuantitiesAsync(partId, -qty, 0, -qty);
        await _txRepo.CreateAsync(new StockTransaction
        {
            PartId = partId, Type = "OUTBOUND", Quantity = qty,
            OperatorId = operatorId, ProjectId = projectId,
            RecipientId = recipientId, RecipientName = recipientName,
            Note = note ?? string.Empty
        });
    }

    public async Task LockAsync(string partId, int qty, string operatorId, string? projectId, string? selectionPlanId, string? selectionItemId, string? note)
    {
        var part = await _partRepo.GetByIdAsync(partId)
            ?? throw new InvalidOperationException("配件不存在");
        if (part.AvailableQty < qty)
            throw new InvalidOperationException($"可用库存不足，当前可用：{part.AvailableQty}");

        await _partRepo.UpdateQuantitiesAsync(partId, -qty, qty, 0);
        await _txRepo.CreateAsync(new StockTransaction
        {
            PartId = partId, Type = "LOCK", Quantity = qty,
            OperatorId = operatorId, ProjectId = projectId,
            SelectionPlanId = selectionPlanId,
            SelectionItemId = selectionItemId,
            Note = note ?? string.Empty
        });
    }

    public async Task UnlockAsync(string partId, int qty, string operatorId, string? projectId, string? note)
    {
        var part = await _partRepo.GetByIdAsync(partId)
            ?? throw new InvalidOperationException("配件不存在");
        if (part.LockedQty < qty)
            throw new InvalidOperationException($"锁定库存不足，当前锁定：{part.LockedQty}");

        await _partRepo.UpdateQuantitiesAsync(partId, qty, -qty, 0);
        await _txRepo.CreateAsync(new StockTransaction
        {
            PartId = partId, Type = "UNLOCK", Quantity = qty,
            OperatorId = operatorId, ProjectId = projectId,
            Note = note ?? string.Empty
        });
    }

    public async Task ReturnAsync(string partId, int qty, string operatorId, string? projectId, string? note)
    {
        await _partRepo.UpdateQuantitiesAsync(partId, qty, 0, qty);
        await _txRepo.CreateAsync(new StockTransaction
        {
            PartId = partId, Type = "RETURN", Quantity = qty,
            OperatorId = operatorId, ProjectId = projectId,
            Note = note ?? string.Empty
        });
    }
}
