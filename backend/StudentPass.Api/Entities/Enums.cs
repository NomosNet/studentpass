namespace StudentPass.Api.Entities;

public enum UserRole
{
    User,
    Admin,
    Partner
}

public enum PartnerRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public static class EnumMapping
{
    public static string ToApi(UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Partner => "partner",
        _ => "user"
    };

    public static UserRole ParseUserRole(string? value) => value?.ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "partner" => UserRole.Partner,
        _ => UserRole.User
    };

    public static string ToApi(PartnerRequestStatus status) => status switch
    {
        PartnerRequestStatus.Approved => "approved",
        PartnerRequestStatus.Rejected => "rejected",
        _ => "pending"
    };

    public static PartnerRequestStatus? ParsePartnerRequestStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "approved" => PartnerRequestStatus.Approved,
        "rejected" => PartnerRequestStatus.Rejected,
        "pending" => PartnerRequestStatus.Pending,
        _ => null
    };
}
