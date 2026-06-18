using InventorySystem.Core.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/parts")]
[Authorize]
public class PartsController : ControllerBase
{
    private readonly IPartRepository _repo;
    private readonly ISpecTemplateRepository _templateRepo;
    private readonly IPartCategoryRepository _categoryRepo;

    public PartsController(
        IPartRepository repo,
        ISpecTemplateRepository templateRepo,
        IPartCategoryRepository categoryRepo)
    {
        _repo = repo;
        _templateRepo = templateRepo;
        _categoryRepo = categoryRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var part = await _repo.GetByIdAsync(id);
        return part == null ? NotFound() : Ok(part);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] PartSearchRequest request)
    {
        var allParts = await _repo.GetAllAsync();
        var results = allParts.AsEnumerable();

        if (!string.IsNullOrEmpty(request.CategoryPath))
            results = results.Where(p => p.Category.StartsWith(request.CategoryPath + "/") || p.Category == request.CategoryPath);
        else if (!string.IsNullOrEmpty(request.Category))
            results = results.Where(p => p.Category == request.Category);

        if (!string.IsNullOrEmpty(request.Keyword))
        {
            var keyword = request.Keyword.ToLower();
            results = results.Where(p =>
                (p.Name?.ToLower().Contains(keyword) ?? false) ||
                (p.Model?.ToLower().Contains(keyword) ?? false) ||
                (p.Description?.ToLower().Contains(keyword) ?? false) ||
                (p.Manufacturer?.ToLower().Contains(keyword) ?? false) ||
                (p.Brand?.ToLower().Contains(keyword) ?? false) ||
                (p.Tags?.Any(t => t.ToLower().Contains(keyword)) ?? false));
        }

        if (request.SpecFilters != null && request.SpecFilters.Count > 0)
        {
            results = results.Where(p => request.SpecFilters.All(sf =>
            {
                var spec = p.Specs.FirstOrDefault(s => s.Key == sf.Key);
                if (spec == null) return false;
                return sf.Op.ToLower() switch
                {
                    "eq" => spec.Value == sf.Value,
                    "ne" => spec.Value != sf.Value,
                    "contains" => spec.Value.Contains(sf.Value, StringComparison.OrdinalIgnoreCase),
                    "gte" => double.TryParse(spec.Value, out var sv) && double.TryParse(sf.Value, out var cv) && sv >= cv,
                    "lte" => double.TryParse(spec.Value, out var sv2) && double.TryParse(sf.Value, out var cv2) && sv2 <= cv2,
                    "gt" => double.TryParse(spec.Value, out var sv3) && double.TryParse(sf.Value, out var cv3) && sv3 > cv3,
                    "lt" => double.TryParse(spec.Value, out var sv4) && double.TryParse(sf.Value, out var cv4) && sv4 < cv4,
                    _ => false
                };
            }));
        }

        if (request.MinAvailableQty.HasValue) results = results.Where(p => p.AvailableQty >= request.MinAvailableQty.Value);
        if (request.MaxAvailableQty.HasValue) results = results.Where(p => p.AvailableQty <= request.MaxAvailableQty.Value);

        return Ok(results.ToList());
    }

    [HttpGet("filter")]
    public async Task<IActionResult> Filter([FromQuery] string? category, [FromQuery] string? criteria)
    {
        var parsedCriteria = new List<(string key, string op, string value)>();
        if (!string.IsNullOrEmpty(criteria))
        {
            foreach (var item in criteria.Split(','))
            {
                var parts = item.Split(':');
                if (parts.Length == 3) parsedCriteria.Add((parts[0], parts[1], parts[2]));
            }
        }
        var result = await _repo.FilterBySpecsAsync(category, parsedCriteria);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Create([FromBody] Part part)
    {
        if (!string.IsNullOrEmpty(part.Category))
        {
            var category = await _categoryRepo.GetByPathAsync(part.Category);
            if (category == null) return BadRequest(new { message = $"分类 '{part.Category}' 不存在，请先创建分类" });
        }
        else return BadRequest(new { message = "必须选择分类才能创建配件" });

        part.TotalQty = 0; part.LockedQty = 0;
        part.CreatedAt = DateTime.UtcNow; part.UpdatedAt = DateTime.UtcNow;
        await _repo.CreateAsync(part);
        return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Update(string id, [FromBody] Part part)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        part.Id = id;
        part.TotalQty = existing.TotalQty; part.LockedQty = existing.LockedQty;
        part.Version = existing.Version;
        part.CreatedAt = existing.CreatedAt; part.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(id, part);
        return Ok(part);
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
