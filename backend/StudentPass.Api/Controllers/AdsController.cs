using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Data;
using StudentPass.Api.Dtos;
using StudentPass.Api.Entities;
using StudentPass.Api.Services;

namespace StudentPass.Api.Controllers;

[ApiController]
[Route("api/v1/ads")]
public class AdsController(AppDbContext db) : ControllerBase
{
  [HttpGet]
  [Authorize]
  public async Task<ActionResult<AdListResponse>> GetAds(
    [FromQuery] string? category,
    [FromQuery] string? search,
    [FromQuery] string? sort,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
  {
    page = Math.Max(1, page);
    limit = Math.Clamp(limit, 1, 100);

    var email = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var now = DateTime.UtcNow;

    var query = db.Ads.AsNoTracking()
      .Include(a => a.Categories)
      .Include(a => a.Partner)
      .Where(a => a.IsActive && a.EndDate > now);

    if (!string.IsNullOrWhiteSpace(category))
      query = query.Where(a => a.Categories.Any(c => c.Name == category));

    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = search.Trim().ToLowerInvariant();
      query = query.Where(a => a.Title.ToLower().Contains(term));
    }

    query = sort switch
    {
      "newest" => query.OrderByDescending(a => a.CreatedAt),
      "ending_soon" => query.OrderBy(a => a.EndDate),
      "popular" => query.OrderByDescending(a => a.ClicksCount),
      _ => query.OrderByDescending(a => a.Prioritet).ThenByDescending(a => a.CreatedAt)
    };

    var total = await query.CountAsync(ct);
    var ads = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(ct);

    HashSet<int> favorites = [];
    if (!string.IsNullOrEmpty(email))
    {
      var favIds = await db.Favorites.AsNoTracking()
        .Where(f => f.UserEmail == email)
        .Select(f => f.AdId)
        .ToListAsync(ct);
      favorites = favIds.ToHashSet();
    }

    var items = ads.Select(a => AdMapping.ToResponse(a, favorites.Contains(a.Id))).ToList();
    var pages = (total + limit - 1) / limit;
    return Ok(new AdListResponse(items, total, page, limit, pages));
  }

  [HttpGet("categories")]
  [AllowAnonymous]
  public async Task<ActionResult<List<CategoryResponse>>> GetCategories(CancellationToken ct)
  {
    var categories = await db.Categories.AsNoTracking()
      .OrderBy(c => c.Name)
      .Select(c => new CategoryResponse(c.Id, c.Name, c.IsCustom))
      .ToListAsync(ct);
    return Ok(categories);
  }

  [HttpGet("{adId:int}")]
  [Authorize]
  public async Task<ActionResult<AdDetailResponse>> GetAd(int adId, CancellationToken ct)
  {
    var now = DateTime.UtcNow;
    var ad = await db.Ads.AsNoTracking()
      .Include(a => a.Categories)
      .Include(a => a.Partner)
      .FirstOrDefaultAsync(a => a.Id == adId && a.IsActive && a.EndDate > now, ct);

    if (ad is null)
      return NotFound(new { detail = "Объявление не найдено" });

    var email = User.FindFirst("sub")?.Value;
    var isFavorite = false;
    if (!string.IsNullOrEmpty(email))
      isFavorite = await db.Favorites.AnyAsync(f => f.UserEmail == email && f.AdId == adId, ct);

    return Ok(AdMapping.ToDetailResponse(ad, isFavorite));
  }

  [HttpPost("{adId:int}/click")]
  [AllowAnonymous]
  public async Task<IActionResult> ClickAd(int adId, CancellationToken ct)
  {
    var ad = await db.Ads.FirstOrDefaultAsync(a => a.Id == adId, ct);
    if (ad is null)
      return NotFound(new { detail = "Объявление не найдено" });

    ad.ClicksCount++;
    ad.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Redirect(ad.Url);
  }
}
