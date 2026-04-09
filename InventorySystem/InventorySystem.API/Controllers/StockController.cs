using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using InventorySystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly IStockTransactionRepository _txRepo;
    private readonly IPartRepository _partRepo;
    private readonly IUserRepository _userRepo;
    private readonly IProjectNodeRepository _projectNodeRepo;
    private readonly ISelectionPlanRepository _selectionPlanRepo;
    private readonly IPurchaseTaskRepository _purchaseTaskRepo;

    public StockController(
        IStockService stockService,
        IStockTransactionRepository txRepo,
        IPartRepository partRepo,
        IUserRepository userRepo,
        IProjectNodeRepository projectNodeRepo,
        ISelectionPlanRepository selectionPlanRepo,
        IPurchaseTaskRepository purchaseTaskRepo)
    {
        _stockService = stockService;
        _txRepo = txRepo;
        _partRepo = partRepo;
        _userRepo = userRepo;
        _projectNodeRepo = projectNodeRepo;
        _selectionPlanRepo = selectionPlanRepo;
        _purchaseTaskRepo = purchaseTaskRepo;
    }

    /// <summary>库存总览 - 按配件聚合</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var parts = await _partRepo.GetAllAsync();
        var pendingTasks = await _purchaseTaskRepo.GetByStatusAsync(PurchaseTaskStatus.Pending);

        var overview = parts.Select(p =>
        {
            var pending = pendingTasks.Where(t => t.PartId == p.Id).Sum(t => t.RequiredQty);
            return new StockOverviewDto
            {
                PartId = p.Id,
                PartName = p.Name,
                PartModel = p.Model,
                Category = p.Category,
                TotalQty = p.TotalQty,
                LockedQty = p.LockedQty,
                AvailableQty = p.TotalQty - p.LockedQty,
                PendingPurchaseQty = pending,
                UpdatedAt = p.UpdatedAt
            };
        }).ToList();

        return Ok(overview);
    }

    /// <summary>库存流水查询</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionQueryDto query)
    {
        var transactions = await _txRepo.QueryAsync(query);
        var dtos = await EnrichTransactionDtos(transactions);
        return Ok(dtos);
    }

    /// <summary>获取单个配件的完整流水</summary>
    [HttpGet("transactions/{partId}")]
    public async Task<IActionResult> GetTransactionsByPartId(string partId)
    {
        var transactions = await _txRepo.GetByPartIdAsync(partId);
        var dtos = await EnrichTransactionDtos(transactions);
        return Ok(dtos);
    }

    /// <summary>锁定状态汇总</summary>
    [HttpGet("locks")]
    public async Task<IActionResult> GetLockSummary()
    {
        var parts = await _partRepo.GetAllAsync();
        var result = new List<StockLockSummaryDto>();

        foreach (var part in parts.Where(p => p.LockedQty > 0))
        {
            var locks = await _txRepo.GetLocksByPartIdAsync(part.Id);
            var enriched = await EnrichLockDetails(locks);

            result.Add(new StockLockSummaryDto
            {
                PartId = part.Id,
                PartName = part.Name,
                PartModel = part.Model,
                Category = part.Category,
                TotalLocked = part.LockedQty,
                Locks = enriched
            });
        }

        return Ok(result);
    }

    /// <summary>单个配件的锁定明细</summary>
    [HttpGet("locks/{partId}")]
    public async Task<IActionResult> GetLocksByPartId(string partId)
    {
        var part = await _partRepo.GetByIdAsync(partId);
        if (part == null) return NotFound();

        var locks = await _txRepo.GetLocksByPartIdAsync(partId);
        var enriched = await EnrichLockDetails(locks);

        return Ok(new StockLockSummaryDto
        {
            PartId = part.Id,
            PartName = part.Name,
            PartModel = part.Model,
            Category = part.Category,
            TotalLocked = part.LockedQty,
            Locks = enriched
        });
    }

    /// <summary>入库</summary>
    [HttpPost("inbound")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Inbound([FromBody] InboundRequestDto req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _stockService.InboundAsync(req.PartId, req.Quantity, operatorId, req.Note);
        return Ok(new { message = "入库成功" });
    }

    /// <summary>出库（必须关联项目）</summary>
    [HttpPost("outbound")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Outbound([FromBody] OutboundRequestDto req)
    {
        if (string.IsNullOrEmpty(req.ProjectId))
            return BadRequest(new { message = "出库必须关联项目" });

        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        try
        {
            await _stockService.OutboundAsync(req.PartId, req.Quantity, operatorId, req.ProjectId, req.RecipientId, req.RecipientName, req.Note);
            return Ok(new { message = "出库成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>锁定</summary>
    [HttpPost("lock")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Lock([FromBody] LockRequestDto req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        try
        {
            await _stockService.LockAsync(req.PartId, req.Quantity, operatorId, req.ProjectId, null, null, req.Note);
            return Ok(new { message = "锁定成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>解锁</summary>
    [HttpPost("unlock")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Unlock([FromBody] UnlockRequestDto req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        try
        {
            await _stockService.UnlockAsync(req.PartId, req.Quantity, operatorId, req.ProjectId, req.Note);
            return Ok(new { message = "解锁成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>归还</summary>
    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] UnlockRequestDto req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _stockService.ReturnAsync(req.PartId, req.Quantity, operatorId, req.ProjectId, req.Note);
        return Ok(new { message = "归还成功" });
    }

    /// <summary>修正所有配件的可用数量（重新计算为 TotalQty - LockedQty）</summary>
    [HttpPost("fix-available-qty")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> FixAvailableQty()
    {
        var parts = await _partRepo.GetAllAsync();
        var fixedCount = 0;
        foreach (var part in parts)
        {
            var correctAvail = part.TotalQty - part.LockedQty;
            if (part.AvailableQty != correctAvail)
            {
                part.AvailableQty = correctAvail;
                await _partRepo.UpdateAsync(part.Id, part);
                fixedCount++;
            }
        }
        return Ok(new { message = $"修正了 {fixedCount} 个配件的可用数量" });
    }

    // ========== Private Helper Methods ==========

    private async Task<List<StockTransactionDto>> EnrichTransactionDtos(List<StockTransaction> transactions)
    {
        var result = new List<StockTransactionDto>();
        var userIds = transactions.Select(t => t.OperatorId).Where(id => !string.IsNullOrEmpty(id)).Distinct();
        var projectIds = transactions.Select(t => t.ProjectId).Where(id => !string.IsNullOrEmpty(id)).Distinct();
        var planIds = transactions.Select(t => t.SelectionPlanId).Where(id => !string.IsNullOrEmpty(id)).Distinct();
        var partIds = transactions.Select(t => t.PartId).Distinct();

        var users = (await Task.WhenAll(userIds.Select(id => _userRepo.GetByIdAsync(id!)))).Where(u => u != null).ToDictionary(u => u!.Id, u => u!);
        var projects = (await Task.WhenAll(projectIds.Select(id => _projectNodeRepo.GetByIdAsync(id!)))).Where(p => p != null).ToDictionary(p => p!.Id, p => p!);
        var plans = (await Task.WhenAll(planIds.Select(id => _selectionPlanRepo.GetByIdAsync(id!)))).Where(p => p != null).ToDictionary(p => p!.Id, p => p!);
        var parts = (await Task.WhenAll(partIds.Select(id => _partRepo.GetByIdAsync(id)))).Where(p => p != null).ToDictionary(p => p!.Id, p => p!);

        foreach (var tx in transactions)
        {
            result.Add(new StockTransactionDto
            {
                Id = tx.Id,
                PartId = tx.PartId,
                PartName = parts.GetValueOrDefault(tx.PartId)?.Name ?? "-",
                PartModel = parts.GetValueOrDefault(tx.PartId)?.Model ?? "-",
                Type = tx.Type,
                SourceType = tx.SourceType,
                Quantity = tx.Quantity,
                OperatorId = tx.OperatorId,
                OperatorName = users.GetValueOrDefault(tx.OperatorId)?.DisplayName ?? "-",
                ProjectId = tx.ProjectId,
                ProjectName = projects.GetValueOrDefault(tx.ProjectId ?? "")?.Name,
                SelectionPlanId = tx.SelectionPlanId,
                SelectionPlanName = plans.GetValueOrDefault(tx.SelectionPlanId ?? "")?.Name,
                SelectionItemId = tx.SelectionItemId,
                RecipientId = tx.RecipientId,
                RecipientName = tx.RecipientName,
                Note = tx.Note,
                Supplier = tx.Supplier,
                PurchaseOrderNo = tx.PurchaseOrderNo,
                Usage = tx.Usage,
                CreatedAt = tx.CreatedAt
            });
        }

        return result;
    }

    private async Task<List<LockDetail>> EnrichLockDetails(List<StockTransaction> locks)
    {
        var result = new List<LockDetail>();
        var userIds = locks.Select(l => l.OperatorId).Where(id => !string.IsNullOrEmpty(id)).Distinct();
        var projectIds = locks.Select(l => l.ProjectId).Where(id => !string.IsNullOrEmpty(id)).Distinct();
        var planIds = locks.Select(l => l.SelectionPlanId).Where(id => !string.IsNullOrEmpty(id)).Distinct();

        var users = (await Task.WhenAll(userIds.Select(id => _userRepo.GetByIdAsync(id!)))).Where(u => u != null).ToDictionary(u => u!.Id, u => u!);
        var projects = (await Task.WhenAll(projectIds.Select(id => _projectNodeRepo.GetByIdAsync(id!)))).Where(p => p != null).ToDictionary(p => p!.Id, p => p!);
        var plans = (await Task.WhenAll(planIds.Select(id => _selectionPlanRepo.GetByIdAsync(id!)))).Where(p => p != null).ToDictionary(p => p!.Id, p => p!);

        foreach (var tx in locks)
        {
            result.Add(new LockDetail
            {
                TransactionId = tx.Id,
                ProjectId = tx.ProjectId ?? string.Empty,
                ProjectName = projects.GetValueOrDefault(tx.ProjectId ?? "")?.Name ?? "-",
                SelectionPlanId = tx.SelectionPlanId ?? string.Empty,
                SelectionPlanName = plans.GetValueOrDefault(tx.SelectionPlanId ?? "")?.Name ?? "-",
                SelectionItemId = tx.SelectionItemId ?? string.Empty,
                LockedQty = tx.Quantity,
                OperatorId = tx.OperatorId,
                OperatorName = users.GetValueOrDefault(tx.OperatorId)?.DisplayName ?? "-",
                LockedAt = tx.CreatedAt
            });
        }

        return result;
    }
}
