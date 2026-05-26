using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Data;
using StudentPass.Api.Dtos;
using StudentPass.Api.Entities;
using StudentPass.Api.Services;

namespace StudentPass.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    AppDbContext db,
    TokenService tokenService,
    IEmailSender emailSender,
    ILogger<AuthController> logger) : ControllerBase
{
  private const string AdminBootstrapEmail = "alex.arkhangelskiy@yandex.ru";

  private bool IsSecureRequest() =>
    Request.IsHttps ||
    string.Equals(Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase);

  [HttpPost("send_code")]
  [AllowAnonymous]
  public async Task<ActionResult<MessageResponse>> SendCode([FromBody] SendCodeRequest request, CancellationToken ct)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var existing = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
    if (existing is not null)
    {
      if (!existing.IsActive)
        return BadRequest(new { detail = "Этот email был удалён. Восстановление невозможно, зарегистрируйтесь с другим email" });
      return BadRequest(new { detail = "Пользователь с таким email уже существует" });
    }

    var code = Random.Shared.Next(100000, 999999).ToString();
    db.EmailVerifications.RemoveRange(db.EmailVerifications.Where(v => v.UserEmail == email));
    db.EmailVerifications.Add(new EmailVerification
    {
      UserEmail = email,
      Code = code,
      ExpiresAt = DateTime.UtcNow.AddMinutes(15)
    });
    await db.SaveChangesAsync(ct);

    logger.LogInformation("Код подтверждения для {Email}: {Code}", email, code);
    await emailSender.SendAsync(
      email,
      "Добро пожаловать в StudentPass!",
      $"{code} - Ваш код для регистрации на платформе студенческих скидок StudentPass",
      ct);

    return Ok(new MessageResponse("Код подтверждения отправлен на почту"));
  }

  [HttpPost("register")]
  [AllowAnonymous]
  public async Task<ActionResult<MessageResponse>> Register([FromBody] UserRegisterRequest request, CancellationToken ct)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var code = request.Code.ToString();
    var verification = await db.EmailVerifications.FirstOrDefaultAsync(
      v => v.UserEmail == email && v.Code == code && v.ExpiresAt > DateTime.UtcNow, ct);

    if (verification is null)
      return Ok(new MessageResponse("Код не подходит! Попробуйте еще раз"));

    var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    if (existing is not null)
    {
      if (!existing.IsActive)
        return BadRequest(new { detail = "Этот email был удалён. Восстановление невозможно, зарегистрируйтесь с другим email" });
      return BadRequest(new { detail = "Пользователь с таким email уже существует" });
    }

    var role = email.Equals(AdminBootstrapEmail, StringComparison.OrdinalIgnoreCase)
      ? UserRole.Admin
      : UserRole.User;

    db.Users.Add(new User
    {
      Email = email,
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
      FullName = request.FullName,
      Role = role,
      IsActive = true
    });
    db.EmailVerifications.RemoveRange(db.EmailVerifications.Where(v => v.UserEmail == email));
    await db.SaveChangesAsync(ct);

    return Ok(new MessageResponse("Вы успешно зарегистрировались!"));
  }

  [HttpPost("register-partner")]
  [AllowAnonymous]
  public async Task<ActionResult<MessageResponse>> RegisterPartner([FromBody] UserRegisterPartnerRequest request, CancellationToken ct)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    if (user is null)
      return BadRequest(new { detail = "Вам сначала нужно зарегистрироваться" });

    if (!user.IsActive)
      return BadRequest(new { detail = "Этот email был удалён" });

    var existingRequest = await db.PartnerRequests.AsNoTracking()
      .FirstOrDefaultAsync(r => r.UserEmail == email, ct);
    if (existingRequest is not null)
    {
      return existingRequest.Status switch
      {
        PartnerRequestStatus.Pending => BadRequest(new { detail = "Ваше сотрудничество уже рассматривается!" }),
        PartnerRequestStatus.Rejected => BadRequest(new { detail = "Извините, ваша заявка отклонена!" }),
        _ => BadRequest(new { detail = "Этот аккаунт уже является партнером!" })
      };
    }

    db.PartnerRequests.Add(new PartnerRequest
    {
      UserEmail = email,
      CompanyName = request.CompanyName,
      ContactPerson = request.FullName,
      Phone = request.Phone,
      Description = request.Description,
      Status = PartnerRequestStatus.Pending
    });
    await db.SaveChangesAsync(ct);

    return Ok(new MessageResponse(
      "Заявка на партнёрство отправлена. После одобрения администратором Вы сможете размещать объявления!"));
  }

  [HttpPost("login")]
  [AllowAnonymous]
  public async Task<ActionResult<TokenResponse>> Login([FromBody] UserLoginRequest request, CancellationToken ct)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
      return Unauthorized(new { detail = "Неверный email или пароль" });

    if (!user.IsActive)
      return Unauthorized(new { detail = "Этот аккаунт является удаленным" });

    var token = tokenService.CreateToken(user);
    Response.Cookies.Append("access_token", token, new CookieOptions
    {
      HttpOnly = true,
      Secure = IsSecureRequest(),
      SameSite = SameSiteMode.Lax,
      MaxAge = TimeSpan.FromSeconds(tokenService.GetCookieMaxAgeSeconds()),
      Path = "/"
    });

    return Ok(new TokenResponse(token));
  }

  [HttpPost("logout")]
  [AllowAnonymous]
  public ActionResult<MessageResponse> Logout()
  {
    Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
    return Ok(new MessageResponse("Выход выполнен"));
  }

  [HttpGet("me")]
  [Authorize]
  public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
  {
    var user = await GetCurrentUserAsync(ct);
    if (user is null) return Unauthorized();
    return Ok(ToUserResponse(user));
  }

  [HttpDelete("me")]
  [Authorize]
  public async Task<ActionResult<MessageResponse>> DeleteMe([FromBody] UserLoginRequest request, CancellationToken ct)
  {
    var current = await GetCurrentUserAsync(ct);
    if (current is null) return Unauthorized();

    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
      return Unauthorized(new { detail = "Неверный email или пароль" });

    current.IsActive = false;
    current.DeletedAt = DateTime.UtcNow;
    current.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);

    return Ok(new MessageResponse("Аккаунт удалён. Данные будут храниться 3 месяца"));
  }

  [HttpPost("recover_account")]
  [AllowAnonymous]
  public async Task<ActionResult<MessageResponse>> RecoverAccount([FromBody] UserLoginRequest request, CancellationToken ct)
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
      return Unauthorized(new { detail = "Неверный email или пароль" });

    if (user.IsActive)
      return Ok(new MessageResponse("На данный момент аккаунт не является удаленным!"));

    user.IsActive = true;
    user.DeletedAt = null;
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);

    return Ok(new MessageResponse("Аккаунт восстановлен!"));
  }

  private async Task<User?> GetCurrentUserAsync(CancellationToken ct)
  {
    var email = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(email)) return null;
    return await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
  }

  private static UserResponse ToUserResponse(User user) => new(
    user.Id,
    user.Email,
    user.FullName,
    EnumMapping.ToApi(user.Role),
    user.IsActive,
    user.CreatedAt);
}
