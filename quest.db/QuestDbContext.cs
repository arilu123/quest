using Microsoft.EntityFrameworkCore;

namespace quest.db;

public sealed class QuestDbContext : DbContext
{
    public QuestDbContext(DbContextOptions<QuestDbContext> options) : base(options) { }

    public DbSet<World> Worlds => Set<World>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<World>(e =>
        {
            e.ToTable("worlds");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(120);
            e.Property(x => x.Fate).HasMaxLength(32);
            e.Property(x => x.Pacing).HasMaxLength(32);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CreatedAt);
        });

        b.Entity<Artifact>(e =>
        {
            e.ToTable("artifacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<int>();
            e.Property(x => x.Stage).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.Model).HasMaxLength(128);
            // Prompt and RawResponse intentionally without length limit (text)

            e.HasOne(x => x.World)
                .WithMany(w => w.Artifacts)
                .HasForeignKey(x => x.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.WorldId, x.Kind, x.Status });
            e.HasIndex(x => new { x.WorldId, x.CreatedAt });
        });
    }
}
