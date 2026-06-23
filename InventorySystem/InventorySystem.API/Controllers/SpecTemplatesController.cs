using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

/// <summary>
/// 规格模板管理 — 定义配件分类的规格参数模板，更新时自动级联到关联分类
/// </summary>
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

    /// <summary>获取全部规格模板</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    /// <summary>根据 ID 获取模板详情</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var template = await _repo.GetByIdAsync(id);
        return template == null ? NotFound() : Ok(template);
    }

    /// <summary>根据分类名获取绑定的规格模板</summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var template = await _repo.GetByCategoryAsync(category);
        return template == null ? NotFound() : Ok(template);
    }

    /// <summary>创建规格模板（校验参数 Key 和 Label 非空）</summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] SpecTemplate template)
    {
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

    /// <summary>更新模板（自动级联更新关联分类的规格参数）</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string id, [FromBody] SpecTemplate template)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

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

        var affectedCategories = await _categoryRepo.GetBySpecTemplateIdAsync(id);
        foreach (var cat in affectedCategories)
        {
            cat.SpecParams = template.ParamDefs;
            await _categoryRepo.UpdateAsync(cat.Id, cat);
        }

        _logger.LogInformation("Cascaded spec params update to {Count} categories for template {TemplateId}",
            affectedCategories.Count, id);
        return Ok(template);
    }

    /// <summary>删除规格模板</summary>
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
