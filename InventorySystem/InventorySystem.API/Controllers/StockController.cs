using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
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

    public StockController(IStockService stockService, IStockTransactionRepository txRepo)
    {
        _stockService = stockService;
        _txRepo = txRepo;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] string? partId, [FromQuery] string? projectId)
    {
        if (!string.IsNullOrEmpty(partId))
            return Ok(await _txRepo.GetByPartIdAsync(partId));
        if (!string.IsNullOrEmpty(projectId))
            return Ok(await _txRepo.GetByProjectIdAsync(projectId));
        return Ok(await _txRepo.GetAllAsync());
    }

    [HttpPost("inbound")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Inbound([FromBody] StockRequest req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _stockService.InboundAsync(req.PartId, req.Quantity, operatorId, req.Note);
        return Ok(new { message = "入库成功" });
    }

    [HttpPost("outbound")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Outbound([FromBody] StockRequest req)
    {
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

    [HttpPost("lock")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Lock([FromBody] StockRequest req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        try
        {
            await _stockService.LockAsync(req.PartId, req.Quantity, operatorId, req.ProjectId, req.Note);
            return Ok(new { message = "锁定成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("unlock")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Unlock([FromBody] StockRequest req)
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

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] StockRequest req)
    {
        var operatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        await _stockService.ReturnAsync(req.PartId, req.Quantity, operatorId, req.ProjectId, req.Note);
        return Ok(new { message = "归还成功" });
    }
}

public record StockRequest(
    string PartId,
    int Quantity,
    string? ProjectId,
    string? RecipientId,
    string? RecipientName,
    string? Note
);
