using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize]
public class SpecTemplatesController : ControllerBase
{
    private readonly ISpecTemplateRepository _repo;

    public SpecTemplatesController(ISpecTemplateRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var template = await _repo.GetByIdAsync(id);
        return template == null ? NotFound() : Ok(template);
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var template = await _repo.GetByCategoryAsync(category);
        return template == null ? NotFound() : Ok(template);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] SpecTemplate template)
    {
        await _repo.CreateAsync(template);
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string id, [FromBody] SpecTemplate template)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        template.Id = id;
        await _repo.UpdateAsync(id, template);
        return Ok(template);
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
