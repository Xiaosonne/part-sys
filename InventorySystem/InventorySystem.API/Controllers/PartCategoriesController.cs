using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/part-categories")]
[Authorize]
public class PartCategoriesController : ControllerBase
{
    private readonly IPartCategoryRepository _repo;

    public PartCategoriesController(IPartCategoryRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree() => Ok(await _repo.GetTreeAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var category = await _repo.GetByIdAsync(id);
        return category == null ? NotFound() : Ok(category);
    }

    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetByParentId(string parentId)
    {
        if (parentId == "root")
            return Ok(await _repo.GetByParentIdAsync(null));
        return Ok(await _repo.GetByParentIdAsync(parentId));
    }

    [HttpGet("path/{*path}")]
    public async Task<IActionResult> GetByPath(string path)
    {
        var category = await _repo.GetByPathAsync(path);
        return category == null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Create([FromBody] PartCategory category)
    {
        category.CreatedAt = DateTime.UtcNow;
        await _repo.CreateAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Update(string id, [FromBody] PartCategory category)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        category.Id = id;
        await _repo.UpdateAsync(id, category);
        return Ok(category);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}
