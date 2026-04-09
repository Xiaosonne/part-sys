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

    /// <summary>
    /// 高级搜索 - 支持多级分类、基本信息、规格过滤
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] PartSearchRequest request)
    {
        var allParts = await _repo.GetAllAsync();
        var results = allParts.AsEnumerable();

        // 1. 多级分类过滤 - 支持路径前缀匹配
        if (!string.IsNullOrEmpty(request.CategoryPath))
        {
            results = results.Where(p =>
                p.Category.StartsWith(request.CategoryPath + "/") ||
                p.Category == request.CategoryPath);
        }
        else if (!string.IsNullOrEmpty(request.Category))
        {
            results = results.Where(p => p.Category == request.Category);
        }

        // 2. 基本信息模糊搜索
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

        // 3. 规格精确过滤
        if (request.SpecFilters != null && request.SpecFilters.Count > 0)
        {
            results = results.Where(p =>
                request.SpecFilters.All(sf =>
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

        // 4. 库存数量过滤
        if (request.MinAvailableQty.HasValue)
        {
            results = results.Where(p => p.AvailableQty >= request.MinAvailableQty.Value);
        }
        if (request.MaxAvailableQty.HasValue)
        {
            results = results.Where(p => p.AvailableQty <= request.MaxAvailableQty.Value);
        }

        return Ok(results.ToList());
    }

    [HttpGet("filter")]
    public async Task<IActionResult> Filter([FromQuery] string? category, [FromQuery] string? criteria)
    {
        var parsedCriteria = new List<(string key, string op, string value)>();
        if (!string.IsNullOrEmpty(criteria))
        {
            // Format: key:op:value,key:op:value
            foreach (var item in criteria.Split(','))
            {
                var parts = item.Split(':');
                if (parts.Length == 3)
                    parsedCriteria.Add((parts[0], parts[1], parts[2]));
            }
        }
        var result = await _repo.FilterBySpecsAsync(category, parsedCriteria);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Create([FromBody] Part part)
    {
        // Validate category exists
        if (!string.IsNullOrEmpty(part.Category))
        {
            var category = await _categoryRepo.GetByPathAsync(part.Category);
            if (category == null)
                return BadRequest(new { message = $"分类 '{part.Category}' 不存在，请先创建分类" });
        }
        else
        {
            return BadRequest(new { message = "必须选择分类才能创建配件" });
        }

        part.CreatedAt = DateTime.UtcNow;
        part.UpdatedAt = DateTime.UtcNow;
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
        part.LockedQty = existing.LockedQty;
        part.AvailableQty = part.TotalQty - existing.LockedQty;
        part.UpdatedAt = DateTime.UtcNow;
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

public class PartSearchRequest
{
    /// <summary>
    /// 分类路径，支持多级，如 "电子元器件/微控制器"
    /// </summary>
    public string? CategoryPath { get; set; }

    /// <summary>
    /// 分类名称（精确匹配）
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 关键字模糊搜索（名称、型号、描述、厂商、品牌、标签）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 规格过滤条件
    /// </summary>
    public List<SpecFilter>? SpecFilters { get; set; }

    /// <summary>
    /// 最小可用数量
    /// </summary>
    public int? MinAvailableQty { get; set; }

    /// <summary>
    /// 最大可用数量
    /// </summary>
    public int? MaxAvailableQty { get; set; }
}

public class SpecFilter
{
    public string Key { get; set; } = string.Empty;
    public string Op { get; set; } = "eq"; // eq, ne, contains, gte, lte, gt, lt
    public string Value { get; set; } = string.Empty;
}
