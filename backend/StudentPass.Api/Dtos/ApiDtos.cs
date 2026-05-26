using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StudentPass.Api.Dtos;

public record MessageResponse(string Message);

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType = "bearer");

public record SendCodeRequest(string Email);

public class UserRegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    [Required, MinLength(1)]
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [Required]
    public int Code { get; set; }
}

public class UserRegisterPartnerRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(1)]
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = "";

    [Required, MinLength(1)]
    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = "";

    [Required, MinLength(1)]
    public string Phone { get; set; } = "";

    public string? Description { get; set; }
}

public class UserLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public record UserResponse(
    int Id,
    string Email,
    [property: JsonPropertyName("full_name")] string FullName,
    string Role,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public record CategoryResponse(int Id, string Name, [property: JsonPropertyName("is_custom")] bool IsCustom);

public class CategoryCreateRequest
{
    [Required, MinLength(1)]
    public string Name { get; set; } = "";
}

public class AdCreateRequest
{
    [Required, MinLength(1)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [Range(1, 100)]
    [JsonPropertyName("discount_percent")]
    public int DiscountPercent { get; set; }

    [Required, MinLength(1)]
    public string Url { get; set; } = "";

    [Required, MinLength(1)]
    public string Address { get; set; } = "";

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [MinLength(1)]
    [JsonPropertyName("category_ids")]
    public List<int> CategoryIds { get; set; } = [];

    [JsonPropertyName("emodzi_id")]
    public int? EmodziId { get; set; }

    public int Prioritet { get; set; }
}

public class AdUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }

    [Range(1, 100)]
    [JsonPropertyName("discount_percent")]
    public int? DiscountPercent { get; set; }

    public string? Url { get; set; }
    public string? Address { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("category_ids")]
    public List<int>? CategoryIds { get; set; }

    [JsonPropertyName("emodzi_id")]
    public int? EmodziId { get; set; }

    public int? Prioritet { get; set; }
}

public record AdResponse(
    int Id,
    string Title,
    string? Description,
    [property: JsonPropertyName("discount_percent")] int DiscountPercent,
    string Url,
    string Address,
    [property: JsonPropertyName("end_date")] DateTime EndDate,
    [property: JsonPropertyName("clicks_count")] int ClicksCount,
    [property: JsonPropertyName("partner_email")] string PartnerEmail,
    [property: JsonPropertyName("partner_name")] string PartnerName,
    List<string> Categories,
    [property: JsonPropertyName("is_favorite")] bool IsFavorite,
    [property: JsonPropertyName("emodzi_id")] int? EmodziId,
    int Prioritet);

public record AdDetailResponse(
    int Id,
    string Title,
    string? Description,
    [property: JsonPropertyName("discount_percent")] int DiscountPercent,
    string Url,
    string Address,
    [property: JsonPropertyName("end_date")] DateTime EndDate,
    [property: JsonPropertyName("clicks_count")] int ClicksCount,
    [property: JsonPropertyName("partner_email")] string PartnerEmail,
    [property: JsonPropertyName("partner_name")] string PartnerName,
    List<string> Categories,
    [property: JsonPropertyName("is_favorite")] bool IsFavorite,
    [property: JsonPropertyName("emodzi_id")] int? EmodziId,
    int Prioritet,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);

public record AdListResponse(
    List<AdResponse> Items,
    int Total,
    int Page,
    int Limit,
    int Pages);

public record FavoriteResponse(
    [property: JsonPropertyName("ad_id")] int AdId,
    string Title,
    [property: JsonPropertyName("discount_percent")] int DiscountPercent,
    [property: JsonPropertyName("end_date")] DateTime EndDate,
    [property: JsonPropertyName("partner_name")] string PartnerName);

public record FavoriteListResponse(
    List<FavoriteResponse> Items,
    int Total,
    int Page,
    int Limit);

public record PartnerResponse(
    int Id,
    string Email,
    [property: JsonPropertyName("company_name")] string CompanyName,
    string? Description,
    [property: JsonPropertyName("logo_url")] string? LogoUrl,
    [property: JsonPropertyName("ads_count")] int AdsCount);

public record PartnerAdsResponse(
    List<AdResponse> Items,
    int Total,
    int Page,
    int Limit,
    int Pages,
    [property: JsonPropertyName("ads_used")] int AdsUsed,
    [property: JsonPropertyName("ads_limit")] int AdsLimit);

public record PartnerRequestResponse(
    int Id,
    [property: JsonPropertyName("user_email")] string UserEmail,
    [property: JsonPropertyName("company_name")] string CompanyName,
    [property: JsonPropertyName("contact_person")] string ContactPerson,
    string Phone,
    string? Description,
    string Status,
    [property: JsonPropertyName("admin_comment")] string? AdminComment,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public record AdminUserResponse(
    int Id,
    string Email,
    [property: JsonPropertyName("full_name")] string FullName,
    string Role,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public record AdminUserListResponse(
    List<AdminUserResponse> Items,
    int Total,
    int Page,
    int Limit,
    int Pages);

public record AdminPartnerRequestListResponse(
    List<PartnerRequestResponse> Items,
    int Total,
    int Page,
    int Limit,
    int Pages);
