using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence.Configurations;
using SpendBear.Infrastructure.Core.Data;
using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext : BaseDbContext, INotificationsUnitOfWork
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}