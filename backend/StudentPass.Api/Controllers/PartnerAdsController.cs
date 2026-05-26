using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Data;
using StudentPass.Api.Dtos;
using StudentPass.Api.Entities;
using StudentPass.Api.Services;

namespace StudentPass.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/partner/ads")]
public class PartnerAdsController(AppDbContext db) : ControllerBase
{
  private async Task<Partner?> GetCurrentPartnerAsync(CancellationToken ct)
  {
    var email = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(email)) return null;

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
    if (user is null || user.Role != UserRole.Partner) return null;

    var partner = await db.Partners.FirstOrDefaultAsync(p => p.UserEmail == email, ct);
    if (partner is null || !partner.IsApproved) return null;
    return partner;
  }

  [HttpGet]
  public async Task<ActionResult<PartnerAdsResponse>> GetMyAds(
    [FromQuery] int page = 1,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
  {
    var partner = await GetCurrentPartnerAsync(ct);
    if (partner is null)
      return StatusCode(403, new { detail = "Доступ только для партнёров" });

    page = Math.Max(1, page);
    limit = Math.Clamp(limit, 1, 100);

    var query = db.Ads.AsNoTracking()
      .Include(a => a.Categories)
      .Include(a => a.Partner)
      .Where(a => a.PartnerEmail == partner.UserEmail);

    var total = await query.CountAsync(ct);
    var ads = await query.OrderByDescending(a => a.CreatedAt)
      .Skip((page - 1) * limit)
      .Take(limit)
      .ToListAsync(ct);

    var adsUsed = await db.Ads.CountAsync(a => a.PartnerEmail == partner.UserEmail && a.IsActive, ct);
    var items = ads.Select(a => AdMapping.ToResponse(a)).ToList();
    var pages = (total + limit - 1) / limit;

    return Ok(new PartnerAdsResponse(items, total, page, limit, pages, adsUsed, partner.AdsLimit));
  }

  [HttpPost]
  public async Task<ActionResult<MessageResponse>> CreateAd([FromBody] AdCreateRequest request, CancellationToken ct)
  {
    var partner = await GetCurrentPartnerAsync(ct);
    if (partner is null)
      return StatusCode(403, new { detail = "Доступ только для партнёров" });

    var currentCount = await db.Ads.CountAsync(a => a.PartnerEmail == partner.UserEmail && a.IsActive, ct);
    if (currentCount >= partner.AdsLimit)
      return BadRequest(new { detail = $"Превышен лимит объявлений (максимум {partner.AdsLimit})" });

    foreach (var catId in request.CategoryIds)
    {
      if (!await db.Categories.AnyAsync(c => c.Id == catId, ct))
        return BadRequest(new { detail = $"Категория с id {catId} не найдена" });
    }

    var categories = await db.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToListAsync(ct);
    var ad = new Ad
    {
      PartnerEmail = partner.UserEmail,
      Title = request.Title,
      Description = request.Description,
      DiscountPercent = request.DiscountPercent,
      Url = request.Url,
      Address = request.Address,
      EndDate = request.EndDate.ToUniversalTime(),
      EmodziId = request.EmodziId,
      Prioritet = request.Prioritet,
      IsActive = true,
      Categories = categories
    };

    db.Ads.Add(ad);
    await db.SaveChangesAsync(ct);
    return Ok(new MessageResponse("Объявление создано"));
  }

  [HttpPut("{adId:int}")]
  public async Task<ActionResult<MessageResponse>> UpdateAd(
    int adId,
    [FromBody] AdUpdateRequest request,
    CancellationToken ct)
  {
    var partner = await GetCurrentPartnerAsync(ct);
    if (partner is null)
      return StatusCode(403, new { detail = "Доступ только для партнёров" });

    var ad = await db.Ads.Include(a => a.Categories)
      .FirstOrDefaultAsync(a => a.Id == adId, ct);
    if (ad is null || ad.PartnerEmail != partner.UserEmail)
      return NotFound(new { detail = "Объявление не найдено" });

    if (request.Title is not null) ad.Title = request.Title;
    if (request.Description is not null) ad.Description = request.Description;
    if (request.DiscountPercent is not null) ad.DiscountPercent = request.DiscountPercent.Value;
    if (request.Url is not null) ad.Url = request.Url;
    if (request.Address is not null) ad.Address = request.Address;
    if (request.EndDate is not null) ad.EndDate = request.EndDate.Value.ToUniversalTime();
    if (request.EmodziId is not null) ad.EmodziId = request.EmodziId;
    if (request.Prioritet is not null) ad.Prioritet = request.Prioritet.Value;

    if (request.CategoryIds is not null)
    {
      ad.Categories.Clear();
      var categories = await db.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToListAsync(ct);
      foreach (var category in categories)
        ad.Categories.Add(category);
    }

    ad.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Ok(new MessageResponse("Объявление обновлено"));
  }

  [HttpDelete("{adId:int}")]
  public async Task<ActionResult<MessageResponse>> DeleteAd(int adId, CancellationToken ct)
  {
    var partner = await GetCurrentPartnerAsync(ct);
    if (partner is null)
      return StatusCode(403, new { detail = "Доступ только для партнёров" });

    var ad = await db.Ads.FirstOrDefaultAsync(a => a.Id == adId, ct);
    if (ad is null || ad.PartnerEmail != partner.UserEmail)
      return NotFound(new { detail = "Объявление не найдено" });

    db.Ads.Remove(ad);
    await db.SaveChangesAsync(ct);
    return Ok(new MessageResponse("Объявление удалено"));
  }
}
