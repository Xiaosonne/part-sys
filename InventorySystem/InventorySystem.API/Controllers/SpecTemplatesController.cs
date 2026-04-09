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
    private readonly IPartCategoryRepository _categoryRepo;
    private readonly ILogger<SpecTemplatesController> _logger;

    public SpecTemplatesController(
        ISpecTemplateRepository repo,
        IPartCategoryRepository categoryRepo,
        ILogger<SpecTemplatesController> logger)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _logger = logger;
    }

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
        // Validate paramDefs have non-empty key and label
        if (template.ParamDefs != null)
        {
            foreach (var param in template.ParamDefs)
            {
                if (string.IsNullOrWhiteSpace(param.Key))
                    return BadRequest(new { message = "规格参数Key不能为空" });
                if (string.IsNullOrWhiteSpace(param.Label))
                    return BadRequest(new { message = $"规格参数 '{param.Key}' 的标签(Label)不能为空" });
            }
        }

        await _repo.CreateAsync(template);
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string id, [FromBody] SpecTemplate template)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Validate paramDefs have non-empty key and label
        if (template.ParamDefs != null)
        {
            foreach (var param in template.ParamDefs)
            {
                if (string.IsNullOrWhiteSpace(param.Key))
                    return BadRequest(new { message = "规格参数Key不能为空" });
                if (string.IsNullOrWhiteSpace(param.Label))
                    return BadRequest(new { message = $"规格参数 '{param.Key}' 的标签(Label)不能为空" });
            }
        }

        template.Id = id;
        await _repo.UpdateAsync(id, template);

        // 级联更新所有引用此模板的分类
        var affectedCategories = await _categoryRepo.GetBySpecTemplateIdAsync(id);
        foreach (var cat in affectedCategories)
        {
            cat.SpecParams = template.ParamDefs;
            await _categoryRepo.UpdateAsync(cat.Id, cat);
        }

        _logger.LogInformation(
            "Cascaded spec params update to {Count} categories for template {TemplateId}",
            affectedCategories.Count, id);

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
