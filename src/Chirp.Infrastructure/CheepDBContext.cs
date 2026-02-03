using Chirp.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure
{
    /// <summary>
    /// Represents the database context for the Chirp application, managing database operations
    /// for authors, cheeps, and their relationships.
    /// </summary>
    public class CheepDBContext : IdentityDbContext<Author, IdentityRole<int>, int>
    {
        public DbSet<Cheep> Cheeps { get; set; }

        public DbSet<Author> Authors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheepDBContext"/> class with specified options.
        /// </summary>
        /// <param name="dbContextOptions">The options to configure the context.</param>
        public CheepDBContext(DbContextOptions<CheepDBContext> dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Configures relationships, indexes, and constraints for the Chirp database model.
        /// Includes unique constraints, cascading behaviors, and many-to-many relationships.
        /// </summary>
        /// <param name="modelBuilder">The builder used to construct the model for the database.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Enforce unique constraint on Author.Name and Author.Email to ensure no duplicates.
            modelBuilder.Entity<Author>()
                .HasIndex(a => a.Name)
                .IsUnique();
            modelBuilder.Entity<Author>()
                .HasIndex(a => a.Email)
                .IsUnique();

            // Define one-to-many relationship between Cheeps and Authors.
            modelBuilder.Entity<Cheep>()
                .HasOne(c => c.Author)
                .WithMany(a => a.Cheeps)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure many-to-many relationship between Cheeps and Authors who like them.
            modelBuilder.Entity<Cheep>()
                .HasMany(c => c.LikedByAuthors) // Cheep has many Authors who liked it
                .WithMany(a => a.LikedCheeps)  // Author can like many Cheeps
                .UsingEntity(j => j.ToTable("AuthorLikedCheeps"));  // Join table

            // Configure many-to-many relationship for following authors.
            modelBuilder.Entity<Author>()
                .HasMany(a => a.FollowedAuthors) // Each Author can follow many other Authors
                .WithMany(a => a.Followers) // Each Author can be followed by many Authors
                .UsingEntity<Dictionary<string, object>>(
                    "AuthorFollows",
                    j => j.HasOne<Author>()
                        .WithMany()
                        .HasForeignKey("FollowedId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j => j.HasOne<Author>()
                        .WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.Cascade));

            // Limit the maximum length of Cheep text to align with application design.
            modelBuilder.Entity<Cheep>()
                .Property(c => c.Text)
                .HasMaxLength(160);

            base.OnModelCreating(modelBuilder);
        }
    }
}