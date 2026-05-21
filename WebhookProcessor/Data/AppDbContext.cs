using Microsoft.EntityFrameworkCore;
using WebhookProcessor.Models;

namespace WebhookProcessor.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();

            // Índice para el worker: sólo trae Pending sin lock activo
            e.HasIndex(x => new { x.Status, x.LockedUntil })
             .HasDatabaseName("IX_OutboxEvents_Status_LockedUntil");

            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });
    }
}
