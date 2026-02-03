namespace Chirp.Core;

/// <summary>
/// Represents an author in a simplified manner, used for client-facing operations where 
/// only the author's name is required.
/// </summary>
public class AuthorDTO
{
    public string? Name { get; set; }
}