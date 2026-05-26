using StudentPass.Api.Dtos;
using StudentPass.Api.Entities;

namespace StudentPass.Api.Services;

public static class AdMapping
{
    public static AdResponse ToResponse(Ad ad, bool isFavorite = false) => new(
        ad.Id,
        ad.Title,
        ad.Description,
        ad.DiscountPercent,
        ad.Url,
        ad.Address,
        ad.EndDate,
        ad.ClicksCount,
        ad.PartnerEmail,
        ad.Partner.CompanyName,
        ad.Categories.Select(c => c.Name).ToList(),
        isFavorite,
        ad.EmodziId,
        ad.Prioritet);

    public static AdDetailResponse ToDetailResponse(Ad ad, bool isFavorite = false) => new(
        ad.Id,
        ad.Title,
        ad.Description,
        ad.DiscountPercent,
        ad.Url,
        ad.Address,
        ad.EndDate,
        ad.ClicksCount,
        ad.PartnerEmail,
        ad.Partner.CompanyName,
        ad.Categories.Select(c => c.Name).ToList(),
        isFavorite,
        ad.EmodziId,
        ad.Prioritet,
        ad.CreatedAt,
        ad.UpdatedAt);
}
