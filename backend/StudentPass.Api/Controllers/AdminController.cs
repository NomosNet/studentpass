using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Data;
using StudentPass.Api.Dtos;
using StudentPass.Api.Entities;

namespace StudentPass.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
  private async Task<User?> GetAdminAsync(CancellationToken ct)
  {
    var email = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(email)) return null;
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
    if (user is null || user.Role != UserRole.Admin) return null;
    return user;
  }

  [HttpGet("partner-requests")]
  public async Task<ActionResult<AdminPartnerRequestListResponse>> GetPartnerRequests(
    [FromQuery] string? status,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
  {
    if (await GetAdminAsync(ct) is null)
      return StatusCode(403, new { detail = "Доступ только для администраторов" });

    page = Math.Max(1, page);
    limit = Math.Clamp(limit, 1, 100);

    var query = db.PartnerRequests.AsNoTracking();
    var parsedStatus = EnumMapping.ParsePartnerRequestStatus(status);
    if (parsedStatus is not null)
      query = query.Where(r => r.Status == parsedStatus);

    var total = await query.CountAsync(ct);
    var requests = await query.OrderByDescending(r => r.CreatedAt)
      .Skip((page - 1) * limit)
      .Take(limit)
      .ToListAsync(ct);

    var items = requests.Select(r => new PartnerRequestResponse(
      r.Id,
      r.UserEmail,
      r.CompanyName,
      r.ContactPerson,
      r.Phone,
      r.Description,
      EnumMapping.ToApi(r.Status),
      r.AdminComment,
      r.CreatedAt)).ToList();

    var pages = (total + limit - 1) / limit;
    return Ok(new AdminPartnerRequestListResponse(items, total, page, limit, pages));
  }

  [HttpPost("partner-requests/{userEmail}")]
  public async Task<ActionResult<MessageResponse>> ApprovePartnerRequest(string userEmail, CancellationToken ct)
  {
    if (await GetAdminAsync(ct) is null)
      return StatusCode(403, new { detail = "Доступ только для администраторов" });

    userEmail = Uri.UnescapeDataString(userEmail).Trim().ToLowerInvariant();
    var partnerRequest = await db.PartnerRequests.FirstOrDefaultAsync(r => r.UserEmail == userEmail, ct);
    if (partnerRequest is null)
      return NotFound(new { detail = "Заявка не найдена" });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == userEmail, ct);
    if (user is null)
      return NotFound(new { detail = "Пользователь не найден" });

    user.Role = UserRole.Partner;
    user.UpdatedAt = DateTime.UtcNow;

    if (!await db.Partners.AnyAsync(p => p.UserEmail == userEmail, ct))
    {
      db.Partners.Add(new Partner
      {
        UserEmail = userEmail,
        CompanyName = partnerRequest.CompanyName,
        Description = partnerRequest.Description,
        IsApproved = true,
        AdsLimit = 5
      });
    }

    db.PartnerRequests.Remove(partnerRequest);
    await db.SaveChangesAsync(ct);

    return Ok(new MessageResponse("Заявка обработана"));
  }

  [HttpGet("users")]
  public async Task<ActionResult<AdminUserListResponse>> GetUsers(
    [FromQuery] string? role,
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
  {
    if (await GetAdminAsync(ct) is null)
      return StatusCode(403, new { detail = "Доступ только для администраторов" });

    page = Math.Max(1, page);
    limit = Math.Clamp(limit, 1, 100);

    var query = db.Users.AsNoTracking().Where(u => u.IsActive);
    if (!string.IsNullOrWhiteSpace(role))
      query = query.Where(u => u.Role == EnumMapping.ParseUserRole(role));

    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = search.Trim().ToLowerInvariant();
      query = query.Where(u =>
        u.Email.ToLower().Contains(term) ||
        u.FullName.ToLower().Contains(term));
    }

    var total = await query.CountAsync(ct);
    var users = await query.OrderByDescending(u => u.CreatedAt)
      .Skip((page - 1) * limit)
      .Take(limit)
      .ToListAsync(ct);

    var items = users.Select(u => new AdminUserResponse(
      u.Id,
      u.Email,
      u.FullName,
      EnumMapping.ToApi(u.Role),
      u.IsActive,
      u.CreatedAt)).ToList();

    var pages = (total + limit - 1) / limit;
    return Ok(new AdminUserListResponse(items, total, page, limit, pages));
  }

  [HttpDelete("users/{userId:int}")]
  public async Task<ActionResult<MessageResponse>> DeleteUser(int userId, CancellationToken ct)
  {
    if (await GetAdminAsync(ct) is null)
      return StatusCode(403, new { detail = "Доступ только для администраторов" });

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    if (user is null)
      return NotFound(new { detail = "Пользователь не найден" });

    db.Users.Remove(user);
    await db.SaveChangesAsync(ct);
    return Ok(new MessageResponse("Пользователь был полностью удален"));
  }

  [HttpPost("categories")]
  public async Task<ActionResult<CategoryResponse>> CreateCategory(
    [FromBody] CategoryCreateRequest request,
    CancellationToken ct)
  {
    if (await GetAdminAsync(ct) is null)
      return StatusCode(403, new { detail = "Доступ только для администраторов" });

    var name = request.Name.Trim();
    if (await db.Categories.AnyAsync(c => c.Name == name, ct))
      return BadRequest(new { detail = "Категория уже существует" });

    var category = new Category { Name = name, IsCustom = false };
    db.Categories.Add(category);
    await db.SaveChangesAsync(ct);
    return Ok(new CategoryResponse(category.Id, category.Name, category.IsCustom));
  }
}
