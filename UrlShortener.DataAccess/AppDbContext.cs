using Microsoft.EntityFrameworkCore;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<UserDbTable> Users => Set<UserDbTable>();
    public DbSet<PlanDbTable> Plans => Set<PlanDbTable>();
    public DbSet<SubscriptionDbTable> Subscriptions => Set<SubscriptionDbTable>();
    public DbSet<ShortLinkDbTable> ShortLinks => Set<ShortLinkDbTable>();
    public DbSet<QrCodeDbTable> QrCodes => Set<QrCodeDbTable>();
    public DbSet<LinkClickDbTable> LinkClicks => Set<LinkClickDbTable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortLinkDbTable>()
            .HasOne(x => x.QrCode)
            .WithOne(x => x.ShortLink)
            .HasForeignKey<QrCodeDbTable>(x => x.ShortLinkId);

        modelBuilder.Entity<LinkClickDbTable>()
            .HasOne(x => x.ShortLink)
            .WithMany(x => x.LinkClicks)
            .HasForeignKey(x => x.ShortLinkId);

        modelBuilder.Entity<ShortLinkDbTable>()
            .HasOne(x => x.User)
            .WithMany(x => x.ShortLinks)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<SubscriptionDbTable>()
            .HasOne(x => x.User)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<SubscriptionDbTable>()
            .HasOne(x => x.Plan)
            .WithMany(x => x.Subscriptions)
            .HasForeignKey(x => x.PlanId);
    }
}