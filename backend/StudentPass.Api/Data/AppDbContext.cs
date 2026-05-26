using Microsoft.EntityFrameworkCore;
using StudentPass.Api.Entities;

namespace StudentPass.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<PartnerRequest> PartnerRequests => Set<PartnerRequest>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<EmailVerification>(e =>
        {
            e.ToTable("email_verifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(6);
        });

        modelBuilder.Entity<PartnerRequest>(e =>
        {
            e.ToTable("partner_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Partner>(e =>
        {
            e.ToTable("partners");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserEmail).IsUnique();
            e.HasOne(x => x.User)
                .WithOne(x => x.Partner)
                .HasForeignKey<Partner>(x => x.UserEmail)
                .HasPrincipalKey<User>(x => x.Email)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Ad>(e =>
        {
            e.ToTable("ads");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Partner)
                .WithMany(x => x.Ads)
                .HasForeignKey(x => x.PartnerEmail)
                .HasPrincipalKey(x => x.UserEmail)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Categories)
                .WithMany(x => x.Ads)
                .UsingEntity(j => j.ToTable("ad_categories"));
        });

        modelBuilder.Entity<Favorite>(e =>
        {
            e.ToTable("favorites");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.UserEmail)
                .HasPrincipalKey(x => x.Email)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Ad)
                .WithMany(x => x.Favorites)
                .HasForeignKey(x => x.AdId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
