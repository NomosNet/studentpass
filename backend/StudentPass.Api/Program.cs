using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentPass.Api.Data;
using StudentPass.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
  .AddJsonOptions(options =>
  {
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

var connectionString = builder.Configuration.GetConnectionString("Default")
  ?? "Server=localhost;Port=3306;Database=studentpass;User=studentpass;Password=devpassword;";
var serverVersion = ServerVersion.Parse("8.4.0-mysql");

builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    mySqlOptions.EnableRetryOnFailure(
      maxRetryCount: 10,
      maxRetryDelay: TimeSpan.FromSeconds(5),
      errorNumbersToAdd: null)));

builder.Services.AddScoped<TokenService>();

var smtpHost = builder.Configuration["Smtp:Host"];
if (!string.IsNullOrWhiteSpace(smtpHost))
{
  builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
  builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
  builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
}

var jwtSecret = builder.Configuration["Jwt:SecretKey"]
  ?? "dev-secret-change-me-in-production-use-long-random-string";
var jwtAlgorithm = builder.Configuration["Jwt:Algorithm"] ?? "HS256";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = false,
      ValidateAudience = false,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
      ValidAlgorithms = new[] { jwtAlgorithm },
      NameClaimType = JwtRegisteredClaimNames.Sub,
      ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
      OnMessageReceived = context =>
      {
        if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
          context.Token = cookieToken;
        return Task.CompletedTask;
      }
    };
  });

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.KnownNetworks.Clear();
  options.KnownProxies.Clear();
});

var devCorsOrigins = new[]
{
  "http://localhost:5173",
  "http://localhost:3000",
  "http://127.0.0.1:5173"
};
var corsOrigins = builder.Configuration["Cors:Origins"]?
  .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
  ?? Array.Empty<string>();
if (corsOrigins.Length == 0)
  corsOrigins = devCorsOrigins;
else
  corsOrigins = corsOrigins.Concat(devCorsOrigins).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.WithOrigins(corsOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
  });
});

var app = builder.Build();

if (app.Environment.IsProduction())
{
  app.UseForwardedHeaders();
}

using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  await db.Database.EnsureCreatedAsync();
  await SeedData.EnsureSeedAsync(db);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
