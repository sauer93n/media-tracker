using Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ReviewEntityTypeConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder
            .Property(r => r.Content)
            .IsRequired();

        builder
            .Property(r => r.AuthorId)
            .IsRequired();

        builder
            .Property(r => r.ReferenceId)
            .IsRequired();

        builder
            .Property(r => r.ReferenceType)
            .HasConversion<string>();

        builder
            .Property(r => r.Id)
            .IsRequired();

        builder
            .Property(r => r.Rating)
            .IsRequired();

        builder
            .HasMany(r => r.Likes)
            .WithOne()
            .HasForeignKey(l => l.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(r => r.Dislikes)
            .WithOne()
            .HasForeignKey(d => d.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}