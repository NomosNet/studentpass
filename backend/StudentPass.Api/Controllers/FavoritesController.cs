using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Data;
using StudentPass.Api.Dtos;
using StudentPass.Api.Entities;

namespace StudentPass.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/favorites")]
public class FavoritesController(AppDbContext db) : ControllerBase
{
  private string? UserEmail =>
    User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

  [HttpGet]
  public async Task<ActionResult<FavoriteListResponse>> GetFavorites(
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
  {
    var email = UserEmail;
    if (string.IsNullOrEmpty(email)) return Unauthorized();

    page = Math.Max(1, page);
    limit = Math.Clamp(limit, 1, 100);
    var now = DateTime.UtcNow;

    var favoritesQuery = db.Favorites.AsNoTracking().Where(f => f.UserEmail == email);
    var total = await favoritesQuery.CountAsync(ct);

    var favoriteRows = await favoritesQuery
      .OrderByDescending(f => f.CreatedAt)
      .Skip((page - 1) * limit)
      .Take(limit)
      .ToListAsync(ct);

    var adIds = favoriteRows.Select(f => f.AdId).ToList();
    var ads = await db.Ads.AsNoTracking()
      .Include(a => a.Partner)
      .Where(a => adIds.Contains(a.Id) && a.IsActive && a.EndDate > now)
      .ToListAsync(ct);
    var adsById = ads.ToDictionary(a => a.Id);

    var items = favoriteRows
      .Where(f => adsById.ContainsKey(f.AdId))
      .Select(f =>
      {
        var ad = adsById[f.AdId];
        return new FavoriteResponse(ad.Id, ad.Title, ad.DiscountPercent, ad.EndDate, ad.Partner.CompanyName);
      })
      .ToList();

    return Ok(new FavoriteListResponse(items, total, page, limit));
  }

  [HttpPost("{adId:int}")]
  public async Task<ActionResult<MessageResponse>> AddFavorite(int adId, CancellationToken ct)
  {
    var email = UserEmail;
    if (string.IsNullOrEmpty(email)) return Unauthorized();

    var adExists = await db.Ads.AnyAsync(a => a.Id == adId, ct);
    if (!adExists)
      return NotFound(new { detail = "Объявление не найдено" });

    if (await db.Favorites.AnyAsync(f => f.UserEmail == email && f.AdId == adId, ct))
      return BadRequest(new { detail = "Уже в избранном" });

    db.Favorites.Add(new Favorite { UserEmail = email, AdId = adId });
    await db.SaveChangesAsync(ct);
    return Ok(new MessageResponse("Добавлено в избранное"));
  }

  [HttpDelete("{adId:int}")]
  public async Task<ActionResult<MessageResponse>> RemoveFavorite(int adId, CancellationToken ct)
  {
    var email = UserEmail;
    if (string.IsNullOrEmpty(email)) return Unauthorized();

    var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.UserEmail == email && f.AdId == adId, ct);
    if (favorite is null)
      return NotFound(new { detail = "Объявление не найдено в избранном" });

    db.Favorites.Remove(favorite);
    await db.SaveChangesAsync(ct);
    return Ok(new MessageResponse("Удалено из избранного"));
  }
}
