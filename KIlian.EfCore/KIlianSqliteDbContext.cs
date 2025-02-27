using KIlian.EfCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KIlian.EfCore;

public class KIlianSqliteDbContext(DbContextOptions<KIlianSqliteDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(messageBuilder =>
        {
            messageBuilder.Property(message => message.Created)
                .HasConversion<DateTimeOffsetToStringConverter>()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Conversation>();

        modelBuilder.Entity<ConversationTurn>(turnBuilder =>
        {
            turnBuilder.HasIndex(turn => new { turn.ConversationId, turn.Order }).IsUnique();
        });

        modelBuilder.Entity<Log>(logBuilder =>
        {
            logBuilder.Property(log => log.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}