using InventorySystem.Core.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepo;

    public UsersController(IUserRepository userRepo) { _userRepo = userRepo; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepo.GetAllAsync();
        return Ok(users.Select(u => new { u.Id, u.Username, u.DisplayName, u.Email, u.Role, u.IsActive }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Username, user.DisplayName, user.Email, user.Role, user.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var existing = await _userRepo.GetByUsernameAsync(req.Username);
        if (existing != null)
            return BadRequest(new { message = "用户名已存在" });

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            DisplayName = req.DisplayName,
            Email = req.Email,
            Role = req.Role,
            IsActive = req.IsActive
        };
        await _userRepo.CreateAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new { user.Id, user.Username, user.DisplayName, user.Email, user.Role, user.IsActive });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest req)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        user.DisplayName = req.DisplayName ?? user.DisplayName;
        user.Email = req.Email ?? user.Email;
        user.Role = req.Role ?? user.Role;
        user.IsActive = req.IsActive ?? user.IsActive;
        await _userRepo.UpdateAsync(id, user);
        return Ok(new { user.Id, user.Username, user.DisplayName, user.Email, user.Role, user.IsActive });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        await _userRepo.DeleteAsync(id);
        return Ok();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest req)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _userRepo.UpdateAsync(id, user);
        return Ok();
    }
}
