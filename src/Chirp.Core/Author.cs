using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Core
{
    
    /// <summary>
    /// Represents an author (user) in the Chirp application. This class extends <see cref="IdentityUser{TKey}"/> 
    /// to include additional properties specific to the application, such as the author's name, cheeps, 
    /// followed authors, followers, and liked cheeps.
    /// </summary>
    public class Author : IdentityUser<int>
    { 
    public int AuthorId { get; set; }
    public string? Name { get; set; } 
    [NotMapped] 
    public ICollection<Cheep>? Cheeps { get; set; } = new List<Cheep>();
    public List<Author>? FollowedAuthors { get; set; } = new List<Author>();
    public List<Author>? Followers { get; set; } = new List<Author>();
    public List<Cheep>? LikedCheeps { get; set; } = new List<Cheep>();
    }
}