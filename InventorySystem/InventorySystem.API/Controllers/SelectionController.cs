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

    public SelectionController(ISelectionPlanRepository repo, IPartRepository partRepo)
    {
        _repo = repo;
        _partRepo = partRepo;
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
        plan.Status = "draft";
        await _repo.CreateAsync(plan);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SelectionPlan plan)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        plan.Id = id;
        plan.CreatedBy = existing.CreatedBy;
        plan.CreatedAt = existing.CreatedAt;
        await _repo.UpdateAsync(id, plan);
        return Ok(plan);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(string id)
    {
        var plan = await _repo.GetByIdAsync(id);
        if (plan == null) return NotFound();
        plan.Status = "submitted";
        await _repo.UpdateAsync(id, plan);
        return Ok(plan);
    }

    [HttpGet("{id}/match")]
    public async Task<IActionResult> MatchParts(string id, [FromQuery] string itemId)
    {
        var plan = await _repo.GetByIdAsync(id);
        if (plan == null) return NotFound();

        var item = plan.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound(new { message = "条目不存在" });

        var criteria = item.FilterCriteria
            .Select(f => (f.Key, f.Operator, f.Value))
            .ToList();

        var matched = await _partRepo.FilterBySpecsAsync(item.Category, criteria);
        return Ok(matched.Select(p => new
        {
            p.Id, p.Name, p.Model, p.Brand, p.Category,
            p.AvailableQty, p.TotalQty, p.LockedQty, p.Specs
        }));
    }
}
