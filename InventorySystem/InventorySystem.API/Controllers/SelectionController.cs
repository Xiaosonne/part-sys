using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/selections")]
[Authorize]
public class SelectionController : ControllerBase
{
    private readonly ISelectionPlanRepository _repo;
    private readonly IPartRepository _partRepo;
    private readonly ISelectionService _selectionService;

    public SelectionController(
        ISelectionPlanRepository repo, IPartRepository partRepo, ISelectionService selectionService)
    {
        _repo = repo; _partRepo = partRepo; _selectionService = selectionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? projectId)
    {
        if (!string.IsNullOrEmpty(projectId))
            return Ok(await _repo.GetByProjectIdAsync(projectId));
        return Ok(await _repo.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var plan = await _repo.GetByIdAsync(id);
        return plan == null ? NotFound() : Ok(plan);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SelectionPlan plan)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        plan.CreatedBy = userId;
        plan.CreatedAt = DateTime.UtcNow;
        plan.Status = SelectionPlanStatus.Draft;
        await _repo.CreateAsync(plan);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SelectionPlan plan)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.Status != SelectionPlanStatus.Draft)
            return BadRequest(new { message = "只能编辑草稿状态的选型单" });
        plan.Id = id; plan.CreatedBy = existing.CreatedBy; plan.CreatedAt = existing.CreatedAt;
        await _repo.UpdateAsync(id, plan);
        return Ok(plan);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        // 允许删除任意状态的选型单，但需确保已锁定的库存已经解锁
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(string id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _selectionService.SubmitAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{planId}/items/{itemId}/outbound")]
    public async Task<IActionResult> Outbound(
        string planId, string itemId,
        [FromQuery] int qty, [FromQuery] string? recipientId, [FromQuery] string? recipientName)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var plan = await _repo.GetByIdAsync(planId);
            if (plan == null) return NotFound(new { message = "选型单不存在" });
            var result = await _selectionService.OutboundAsync(planId, itemId, qty, userId, plan.ProjectId, recipientId, recipientName);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _selectionService.CancelAsync(id, userId);
            return Ok(new { message = "选型单已取消" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id}/match")]
    public async Task<IActionResult> MatchParts(string id, [FromQuery] string itemId)
    {
        var plan = await _repo.GetByIdAsync(id);
        if (plan == null) return NotFound();
        var item = plan.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound(new { message = "条目不存在" });

        var matched = await _partRepo.FilterBySpecsAsync(item.Category, new());
        return Ok(matched.Select(p => new
        {
            p.Id, p.Name, p.Model, p.Brand, p.Category,
            p.AvailableQty, p.TotalQty, p.LockedQty, p.Specs
        }));
    }
}
