using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentPass.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
  [HttpGet("health")]
  [AllowAnonymous]
  public IActionResult Health() => Ok(new { status = "ok", service = "StudentPass API" });
}
