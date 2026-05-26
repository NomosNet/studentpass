using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StudentPass.Api.Entities;

namespace StudentPass.Api.Services;

public class TokenService(IConfiguration configuration)
{
    public string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new("user_id", user.Id.ToString()),
            new("role", EnumMapping.ToApi(user.Role)),
            new("full_name", user.FullName)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int GetCookieMaxAgeSeconds() =>
        configuration.GetValue("Jwt:AccessTokenMinutes", 60) * 60;

    public string GetSecret() =>
        configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");

    public string GetAlgorithm() => configuration["Jwt:Algorithm"] ?? "HS256";
}
