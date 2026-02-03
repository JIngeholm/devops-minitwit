using System.ComponentModel.DataAnnotations;

namespace Chirp.Core
{
    /// <summary>
    /// Represents a Cheep in a simplified manner, optimized for use in client responses.
    /// Focuses on essential details such as author name, content, and engagement metrics.
    /// </summary>
    public class CheepDTO
    {
        public string? AuthorName { get; set; } 
        [Key]
        public string? Text { get; set; } 
        public string? TimeStamp { get; set; }
        public int Likes { get; set; } = 0;
    }
}