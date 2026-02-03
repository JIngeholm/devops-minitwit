using Chirp.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Distributed;

namespace Chirp.Infrastructure
{
    public interface ICheepRepository
    {
        Task<List<CheepDTO>> GetCheeps(int pageNumber, int pageSize);
        Task SaveCheep(Cheep cheep, Author author);
        Task<List<Cheep>> GetCheepsByAuthor(int authorId);
        Task<bool> DoesUserLikeCheep(Cheep cheep, Author author);
        Task LikeCheep(Cheep cheep, Author author);
        Task UnLikeCheep(Cheep cheep, Author author);
        Task<Cheep?> FindCheep(string text, string timestamp, string authorName);
    }
    /// <summary>
    /// This class handles the handling of the infrastructure of the cheeps.
    /// It acts as the layer between the database and the application logic
    /// </summary>
    public class CheepRepository : ICheepRepository
    {
        public readonly CheepDBContext _dbContext;

        public CheepRepository(CheepDBContext dbContext)
        {
            _dbContext = dbContext;
            SQLitePCL.Batteries.Init();
        }

        public async Task<List<Cheep>> GetCheepsByAuthor(int authorId)
        {
            return await _dbContext.Cheeps
                .Where(c => c.AuthorId == authorId) // Use the updated property name here
                .OrderByDescending(c => c.TimeStamp)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a paginated list of CheepDTO objects, ordered by the most recent timestamp.
        /// </summary>
        /// <param name="pageNumber">The current page number for pagination, starting from 1.</param>
        /// <param name="pageSize">The number of items to include in a single page.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains a list of CheepDTO objects.
        /// </returns>
        public async Task<List<CheepDTO>> GetCheeps(int pageNumber, int pageSize)
        {
            var cheeps = _dbContext.Cheeps;

            var cheepsQuery = await cheeps.OrderByDescending(cheep => cheep.TimeStamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(cheep => new CheepDTO
                {
                    AuthorName = cheep.Author != null ? cheep.Author.Name : "Unknown",
                    Text = cheep.Text,
                    TimeStamp = cheep.TimeStamp.ToString(),
                    Likes = cheep.Likes,
                })
                .ToListAsync();
            return cheepsQuery;
        }
        
        
        /// <summary>
        /// Saves a new Cheep to the database and reloads the Author's Cheeps collection.
        /// </summary>
        /// <param name="cheep">The Cheep object to be saved to the database.</param>
        /// <param name="author">The Author associated with the Cheep. The Author's Cheeps collection will be reloaded.</param>
        /// <exception cref="InvalidOperationException">Thrown if the Author's Cheeps collection is null.</exception>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SaveCheep(Cheep cheep, Author author)
        {
            if (author.Cheeps == null)
            {
                throw new InvalidOperationException("Author's Cheeps collection is null.");
            }
            
            await _dbContext.Cheeps.AddAsync(cheep);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Entry(author).Collection(a => a.Cheeps!).LoadAsync();
        }
        
        /// <summary>
        /// Checks if the specified Author has liked the given Cheep.
        /// </summary>
        /// <param name="cheep">The Cheep to check for a like.</param>
        /// <param name="author">The Author whose liked Cheeps collection is being checked.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The returned task result is true if the Author has liked the Cheep, else it is false
        /// </returns>
        public Task<bool> DoesUserLikeCheep(Cheep cheep, Author author)
        {
            if (author.LikedCheeps == null)
            {
                return Task.FromResult(false);
            }

            foreach (var likedCheep in author.LikedCheeps)
            {
                if (cheep.Text == likedCheep.Text)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        
        
        /// <summary>
        /// Adds the specified Cheep to the Authors liked Cheeps collection and increments the Cheeps like count.
        /// </summary>
        /// <param name="cheep">The Cheep to be liked.</param>
        /// <param name="author">The Author who is liking the Cheep. The Cheep is added to their liked Cheeps collection if it is not null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task LikeCheep(Cheep cheep, Author author)
        {
            if (author.LikedCheeps != null)
            {
                author.LikedCheeps.Add(cheep);
                cheep.Likes += 1;
            }
            await _dbContext.SaveChangesAsync();
        }
        
        /// <summary>
        /// Removes the specified Cheep to the Authors liked Cheeps collection and decrements the Cheeps like count.
        /// </summary>
        /// <param name="cheep">The Cheep to be unliked.</param>
        /// <param name="author">The Author who is unliking the Cheep. The Cheep is removed from their liked Cheeps collection if it is not null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task UnLikeCheep(Cheep cheep, Author author)
        {
            if (author.LikedCheeps != null)
            {
                author.LikedCheeps.Remove(cheep);
                cheep.Likes -= 1;
            }
            
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Searches for a Cheep based on its text, timestamp, and author's name.
        /// </summary>
        /// <param name="text">The text content of the Cheep to find.</param>
        /// <param name="timestamp">The timestamp of the Cheep as a string. Must be in a valid datetime format.</param>
        /// <param name="authorName">The name of the Author of the Cheep.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result is the Cheep object if found; otherwise, null.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the timestamp is not in a valid datetime format.</exception>
        public async Task<Cheep?> FindCheep(string text, string timestamp, string authorName)
        {
            if (!DateTime.TryParse(timestamp, out var parsedTimestamp))
            {
                throw new ArgumentException("Invalid timestamp format.");
            }
            
            var cheeps = await _dbContext.Cheeps
                .Include(c => c.Author)
                .ToListAsync();
            
            foreach (var cheep in cheeps)
            {
                if (cheep.Text?.ToLower() == text.ToLower() && cheep.TimeStamp.ToString().ToLower() == parsedTimestamp.ToString().ToLower() && cheep.Author != null && cheep.Author.Name?.ToLower() == authorName.ToLower())
                {
                    return cheep;
                }
            }

            return null;
        }
    }
}