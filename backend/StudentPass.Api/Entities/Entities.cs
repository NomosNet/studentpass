namespace StudentPass.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Partner? Partner { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

public class EmailVerification
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string Code { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PartnerRequest
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string ContactPerson { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Description { get; set; }
    public PartnerRequestStatus Status { get; set; } = PartnerRequestStatus.Pending;
    public string? AdminComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Partner
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsApproved { get; set; } = true;
    public int AdsLimit { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsCustom { get; set; }

    public ICollection<Ad> Ads { get; set; } = new List<Ad>();
}

public class Ad
{
    public int Id { get; set; }
    public string PartnerEmail { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int DiscountPercent { get; set; }
    public string Url { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime EndDate { get; set; }
    public int ClicksCount { get; set; }
    public bool IsActive { get; set; } = true;
    public int? EmodziId { get; set; }
    public int Prioritet { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Partner Partner { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

public class Favorite
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public int AdId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Ad Ad { get; set; } = null!;
}
