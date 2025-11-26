using Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class DislikeEntityTypeConfiguration : IEntityTypeConfiguration<Dislike>
{
    public void Configure(EntityTypeBuilder<Dislike> builder)
    {
        builder.HasKey(d => new { d.ReviewId, d.UserId });

        builder.ToTable("Dislikes");
    }
}