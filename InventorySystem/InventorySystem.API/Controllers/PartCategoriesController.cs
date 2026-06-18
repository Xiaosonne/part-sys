using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

/// <summary>
/// 配件分类管理 — 分类树维护、创建/编辑/删除（支持规格模板级联）
/// </summary>
[ApiController]
[Route("api/part-categories")]
[Authorize]
public class PartCategoriesController : ControllerBase
{
    private readonly IPartCategoryRepository _repo;
    private readonly ISpecTemplateRepository _templateRepo;

    public PartCategoriesController(IPartCategoryRepository repo, ISpecTemplateRepository templateRepo)
    {
        _repo = repo;
        _templateRepo = templateRepo;
    }

    /// <summary>获取全部分类列表</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    /// <summary>获取分类树（全量层级）</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree() => Ok(await _repo.GetTreeAsync());

    /// <summary>根据 ID 获取分类</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var category = await _repo.GetByIdAsync(id);
        return category == null ? NotFound() : Ok(category);
    }

    /// <summary>根据父级 ID 获取子分类列表（"root" 表示根节点）</summary>
    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetByParentId(string parentId)
    {
        if (parentId == "root")
            return Ok(await _repo.GetByParentIdAsync(null));
        return Ok(await _repo.GetByParentIdAsync(parentId));
    }

    /// <summary>根据路径获取分类（如 "电子元器件/微控制器"）</summary>
    [HttpGet("path/{*path}")]
    public async Task<IActionResult> GetByPath(string path)
    {
        var category = await _repo.GetByPathAsync(path);
        return category == null ? NotFound() : Ok(category);
    }

    /// <summary>创建分类（支持指定规格模板自动填充规格参数）</summary>
    [HttpPost]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Create([FromBody] PartCategory category)
    {
        var siblings = await _repo.GetByParentIdAsync(category.ParentId);
        if (siblings.Any(s => s.Name == category.Name && s.Id != category.Id))
            return BadRequest(new { message = $"同级分类中已存在名称为 '{category.Name}' 的分类" });

        category.CreatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(category.SpecTemplateId))
        {
            var template = await _templateRepo.GetByIdAsync(category.SpecTemplateId);
            if (template != null) category.SpecParams = template.ParamDefs;
        }

        await _repo.CreateAsync(category);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>更新分类（模板ID变化时自动级联刷新规格参数）</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Update(string id, [FromBody] PartCategory category)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (category.Name != existing.Name)
        {
            var siblings = await _repo.GetByParentIdAsync(existing.ParentId);
            if (siblings.Any(s => s.Name == category.Name && s.Id != id))
                return BadRequest(new { message = $"同级分类中已存在名称为 '{category.Name}' 的分类" });
        }

        if (category.SpecTemplateId != existing.SpecTemplateId)
        {
            if (!string.IsNullOrEmpty(category.SpecTemplateId))
            {
                var template = await _templateRepo.GetByIdAsync(category.SpecTemplateId);
                category.SpecParams = template?.ParamDefs;
            }
            else { category.SpecParams = null; }
        }

        category.Id = id;
        await _repo.UpdateAsync(id, category);
        return Ok(category);
    }

    /// <summary>删除分类</summary>
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
