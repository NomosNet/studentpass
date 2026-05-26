using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Entities;

namespace StudentPass.Api.Data;

public static class SeedData
{
  public static async Task EnsureSeedAsync(AppDbContext db)
  {
    if (await db.Categories.AnyAsync())
      return;

    var defaults = new[]
    {
      "Еда", "Одежда", "Развлечения", "Учёба", "Спорт", "Красота", "Путешествия", "Прочее"
    };

    foreach (var name in defaults)
      db.Categories.Add(new Category { Name = name, IsCustom = false });

    await db.SaveChangesAsync();
  }
}
