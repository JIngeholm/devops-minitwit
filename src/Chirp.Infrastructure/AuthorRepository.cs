using Chirp.Core;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure
{
    public interface IAuthorRepository
    {
        Task<Author> FindAuthorWithName(string userName);
        Task<Author> FindAuthorWithEmail(string email);
        Task<bool> IsFollowingAsync(int followerId, int followedId);
        Task<List<Author>> GetFollowing(int followerId);
        Task<bool> FindIfAuthorExistsWithEmail(string email);
        Task FollowUserAsync(int followerId, int followedId);
        Task UnFollowUserAsync(int followerId, int followedId);
        Task<List<Cheep>> GetLikedCheeps(int userId);
        Task<List<AuthorDTO>> SearchAuthorsAsync(string searchWord);
    }
    /// <summary>
    /// Repository for author-related operations.
    /// </summary>
    public class AuthorRepository : IAuthorRepository
    {
        public readonly CheepDBContext DbContext;

        public AuthorRepository(CheepDBContext dbContext)
        {
            DbContext = dbContext;
            SQLitePCL.Batteries.Init();
        }
        
        /// <summary>
        /// Retrieves an author along with their relationships (e.g., followers, followed authors, liked Cheeps) using a username.
        /// </summary>
        /// <param name="userName">The username of the author to retrieve.</param>
        /// <returns>An <see cref="Author"/> object with associated relationships.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no author with the specified name exists.</exception>
        public async Task<Author> FindAuthorWithName(string userName)
        {
            var author = await DbContext.Authors
                .Include(a => a.FollowedAuthors!)
                .ThenInclude(fa => fa.Cheeps)
                .Include(a => a.Cheeps)
                .Include(a => a.Followers)
                .Include(a => a.LikedCheeps)
                .AsSplitQuery()
                .FirstOrDefaultAsync(author => author.Name == userName);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with name {userName} not found.");
            }

            return author;
        }
        /// <summary>
        /// Finds an author by their email address.
        /// </summary>
        /// <param name="email">The email address of the author.</param>
        /// <returns>An <see cref="Author"/> object if found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no author with the given email exists.</exception>
        public async Task<Author> FindAuthorWithEmail(string email)
        {
            var author = await DbContext.Authors.FirstOrDefaultAsync(author => author.Email == email);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with email {email} not found.");
            }

            return author;
        }
        
        /// <summary>
        /// Checks whether an author exists with the specified email.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns><c>true</c> if an author exists with the specified email; otherwise, <c>false</c>.</returns>
        public async Task<bool> FindIfAuthorExistsWithEmail(string email)
        {
            var author = await DbContext.Authors.FirstOrDefaultAsync(author => author.Email == email);
            if (author == null)
            {
                return false;
            }

            return true;
        }
        
        
        public async Task<Author> FindAuthorWithId(int authorId)
        {
            var author = await DbContext.Authors.FirstOrDefaultAsync(author => author.AuthorId == authorId);
            if (author == null)
            {
                throw new InvalidOperationException($"Author with ID {authorId} was not found.");
            }

            return author;
        }
        
        /// <summary>
        /// Adds a follower-followed relationship between two authors.
        /// </summary>
        /// <param name="followerId">The ID of the user who is following.</param>
        /// <param name="followedId">The ID of the user being followed.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if either the follower or the followed user does not exist or has a null name.
        /// </exception>
        /// <remarks>
        /// This method verifies the relationship using <see cref="IsFollowingAsync"/>.
        /// </remarks>
        public async Task FollowUserAsync(int followerId, int followedId)
        {
            //logged in user
            var follower = await DbContext.Authors.SingleOrDefaultAsync(a => a.AuthorId == followerId);
            //the user that the logged in user wants to follow
            var followed = await DbContext.Authors.SingleOrDefaultAsync(a => a.AuthorId == followedId);
            
            if (follower == null || follower.Name == null)
            {
                throw new InvalidOperationException("Follower or follower's name is null.");
            }

            if (followed == null || followed.Name == null)
            {
                throw new InvalidOperationException("Followed author or followed author's name is null.");
            }
            
            if (!await IsFollowingAsync(followerId, followedId))
            {
                follower.FollowedAuthors?.Add(followed);
                followed.Followers?.Add(follower);
                await DbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Removes a follower-followed relationship between two authors.
        /// </summary>
        /// <param name="followerId">The ID of the user who is unfollowing.</param>
        /// <param name="followedId">The ID of the user being unfollowed.</param>
        /// <remarks>
        /// This method loads the <c>FollowedAuthors</c> list to be able to remove the relationship.
        /// </remarks>
        public async Task UnFollowUserAsync(int followerId, int followedId)
        {
            // The logged in Author
            var follower = await DbContext.Authors
                .Include(a => a.FollowedAuthors) 
                .AsSplitQuery()
                .SingleOrDefaultAsync(a => a.AuthorId == followerId);
        
            // The author whom the logged in author is unfollowing
            var followed = await DbContext.Authors
                .SingleOrDefaultAsync(a => a.AuthorId == followedId);

            if (follower != null && followed != null)
            {
                if (follower.FollowedAuthors?.Contains(followed) == true)
                {
                    follower.FollowedAuthors.Remove(followed);
                    await DbContext.SaveChangesAsync();
                }
            }
        }
        
        /// <summary>
        /// Checks if one user is following another.
        /// </summary>
        /// <param name="followerId">The ID of the user who might be following.</param>
        /// <param name="followedId">The ID of the user who might be followed.</param>
        /// <returns><c>true</c> if the follower-followed relationship exists; otherwise, <c>false</c>.</returns>
        public async Task<bool> IsFollowingAsync(int followerId, int followedId)
        {
            var loggedInUser = await DbContext.Authors.Include(a => a.FollowedAuthors)
                .Include(a => a.FollowedAuthors)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.AuthorId == followerId);

            return loggedInUser?.FollowedAuthors?.Any(f => f.AuthorId == followedId) ?? false;
        }
        
        /// <summary>
        /// Retrieves the list of authors a specific user is following.
        /// </summary>
        /// <param name="followerId">The ID of the user whose following list to retrieve.</param>
        /// <returns>A list of <see cref="Author"/> objects that the user is following.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the user or their following list is null.</exception>
        public async Task<List<Author>> GetFollowing(int followerId)
        {
            var follower = await DbContext.Authors.Include(a => a.FollowedAuthors)
                .Include(a => a.FollowedAuthors)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.AuthorId == followerId);
            if (follower == null || follower.FollowedAuthors == null)
            {
                throw new InvalidOperationException("Follower or followed authors is null.");
            }
            return follower.FollowedAuthors;
        }

        /// <summary>
        /// Retrieves all Cheeps liked by a user.
        /// </summary>
        /// <param name="userId">The ID of the user whose liked Cheeps the method retrieves.</param>
        /// <returns>A list of <see cref="Cheep"/> objects liked by the user.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the user or their liked Cheeps list is null.</exception>
        public async Task<List<Cheep>> GetLikedCheeps(int userId)
        {
            var user = await DbContext.Authors
                .Include(a => a.LikedCheeps)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.AuthorId == userId);
            // user.LikedCheeps cannot be null here because the query ensures it's at least an empty list
            // we added the check so the compiler doesn't give us a warning in the return statement
            if (user == null || user.LikedCheeps == null)
            {
                throw new InvalidOperationException("User liked cheeps is null.");
            }
            return user.LikedCheeps;
        }
        
        /// <summary>
        /// Searches for authors based on a name fragment.
        /// </summary>
        /// <param name="searchWord">The partial or full name of the author to search for.</param>
        /// <returns>A list of <see cref="AuthorDTO"/> objects matching the search criteria.</returns>
        public async Task<List<AuthorDTO>> SearchAuthorsAsync(string searchWord)
        {
            if (string.IsNullOrWhiteSpace(searchWord))
            {
                return new List<AuthorDTO>(); // Return empty list if no search word is provided
            }

            if (searchWord.Length > 2)
            {
                // Perform a case-insensitive search for authors whose name contains the search word
                return await DbContext.Authors
                    .Where(a => EF.Functions.Like(a.Name, $"%{searchWord}%"))
                    .Select(a => new AuthorDTO
                    {
                        Name = a.Name // Map Author entity to AuthorDTO
                    })
                    .ToListAsync();
            }
            else
            {
                return await DbContext.Authors
                    .Where(a => EF.Functions.Like(a.Name, $"{searchWord}%"))
                    .Select(a => new AuthorDTO
                    {
                        Name = a.Name // Map Author entity to AuthorDTO
                    })
                    .ToListAsync();
            }
        }
    }
}