using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Data;
using StudentPass.Api.Dtos;
using StudentPass.Api.Services;

namespace StudentPass.Api.Controllers;

[ApiController]
[Route("api/v1/partners")]
public class PartnersController(AppDbContext db) : ControllerBase
{
  [HttpGet]
  [AllowAnonymous]
  public async Task<ActionResult<List<PartnerResponse>>> GetPartners(
    [FromQuery] string? search,
    [FromQuery] int limit = 100,
    CancellationToken ct = default)
  {
    limit = Math.Clamp(limit, 1, 500);
    var query = db.Partners.AsNoTracking().Where(p => p.IsApproved);

    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = search.Trim().ToLowerInvariant();
      query = query.Where(p => p.CompanyName.ToLower().Contains(term));
    }

    var partners = await query.OrderByDescending(p => p.CreatedAt).Take(limit).ToListAsync(ct);
    var result = new List<PartnerResponse>();
    foreach (var partner in partners)
    {
      var adsCount = await db.Ads.CountAsync(a => a.PartnerEmail == partner.UserEmail && a.IsActive, ct);
      result.Add(new PartnerResponse(
        partner.Id,
        partner.UserEmail,
        partner.CompanyName,
        partner.Description,
        partner.LogoUrl,
        adsCount));
    }

    return Ok(result);
  }

  [HttpGet("{partnerId:int}/ads")]
  [AllowAnonymous]
  public async Task<ActionResult<AdListResponse>> GetPartnerAds(
    int partnerId,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
  {
    page = Math.Max(1, page);
    limit = Math.Clamp(limit, 1, 100);

    var partner = await db.Partners.AsNoTracking()
      .FirstOrDefaultAsync(p => p.Id == partnerId && p.IsApproved, ct);
    if (partner is null)
      return NotFound(new { detail = "Партнёр не найден" });

    var query = db.Ads.AsNoTracking()
      .Include(a => a.Categories)
      .Include(a => a.Partner)
      .Where(a => a.PartnerEmail == partner.UserEmail && a.IsActive);

    var total = await query.CountAsync(ct);
    var ads = await query.OrderByDescending(a => a.CreatedAt)
      .Skip((page - 1) * limit)
      .Take(limit)
      .ToListAsync(ct);

    var items = ads.Select(a => AdMapping.ToResponse(a)).ToList();
    var pages = (total + limit - 1) / limit;
    return Ok(new AdListResponse(items, total, page, limit, pages));
  }
}
