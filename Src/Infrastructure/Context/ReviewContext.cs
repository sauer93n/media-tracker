using Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public class ReviewContext : DbContext
{
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Dislike> Dislikes { get; set; }

    public ReviewContext(DbContextOptions<ReviewContext> options)
        : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewContext).Assembly);
    }
}