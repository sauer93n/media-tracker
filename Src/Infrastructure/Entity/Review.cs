using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Entity;

[EntityTypeConfiguration(typeof(ReviewEntityTypeConfiguration))]
public class Review
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; }

    public double Rating { get; set; }

    public int Likes { get; set; }

    public int Dislikes { get; set; }

    public string ReferenceId { get; set; }
    
    public ReferenceType ReferenceType { get; set; }

    public bool IsDeleted { get; set; }
}