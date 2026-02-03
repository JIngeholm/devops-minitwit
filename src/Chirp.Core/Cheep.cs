using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Chirp.Core;

/// <summary>
/// Represents a "cheep" in the Chirp application.
/// A cheep is a short message posted by an author, similar to a tweet.
/// </summary>
public class Cheep
{
    public int CheepId { get; set; }
    public string? Text { get; set; }
    public DateTime TimeStamp { get; set; }
    
    //Put AuthorId as foreign key form Author. DbInitializer could not handle the value unless this was put
    public int AuthorId { get; set; }
    [JsonIgnore]
    public Author? Author { get; set; }

    public int Likes { get; set; } = 0;
    public List<Author>? LikedByAuthors { get; set; } = new List<Author>();
}