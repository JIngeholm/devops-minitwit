using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class PublicModel : PageModel
{
    public readonly IAuthorRepository AuthorRepository;
    public readonly ICheepRepository CheepRepository;
    public readonly SignInManager<Author> SignInManager;
    public List<CheepDTO> Cheeps { get; set; } = new List<CheepDTO>();
    public  int PageSize = 32;
    public int PageNumber { get; set; }
    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheep cannot be more than 160 characters.")]
    public string? Text { get; set; }
    public List<Author> Authors { get; set; } = new List<Author>();
    public List<Cheep> LikedCheeps { get; set; } = new List<Cheep>();
    public List<Author> FollowedAuthors { get; set; } = new List<Author>();

    public PublicModel(ICheepRepository cheepRepository, IAuthorRepository authorRepository, SignInManager<Author> signInManager)
    {
        CheepRepository = cheepRepository;
        AuthorRepository = authorRepository;
        SignInManager = signInManager;
    }

    /// <summary>
    /// Handles GET requests to display the public timeline and user-specific data if authenticated.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> indicating the result of the operation.</returns>
    /// <remarks>
    /// Authenticated users are validated against the database. If not found, they are signed out and redirected.
    /// </remarks>
    public async Task<ActionResult> OnGet()
    {
        //check if logged-in user exists in database, otherwise log out and redirect to public timeline
        if (SignInManager.IsSignedIn(User) 
            && !string.IsNullOrEmpty(User.Identity?.Name) 
            && await AuthorRepository.FindIfAuthorExistsWithEmail(User.Identity.Name) == false)
        {
            await SignInManager.SignOutAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}"; 
            return Redirect($"{baseUrl}/");
        }
        
        //default to page number 1 if no page is specified
        var pageQuery = Request.Query["page"];
        PageNumber = int.TryParse(pageQuery, out int page) ? page : 1;
        
        Cheeps = await CheepRepository.GetCheeps(PageNumber, PageSize);
        
        if (User.Identity?.IsAuthenticated == true)
        {
            var authorEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(authorEmail))
            {
                var loggedInAuthor = await AuthorRepository.FindAuthorWithEmail(authorEmail);
                FollowedAuthors = await AuthorRepository.GetFollowing(loggedInAuthor.AuthorId);
            }
        }
        return Page();
    }
    
    /// <summary>
    /// Handles POST requests to publish a new cheep by the logged-in user.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after saving the cheep.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPost()
    {
        var authorName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }
        
        var author = await AuthorRepository.FindAuthorWithEmail(authorName);
        var cheep = new Cheep
        {
            AuthorId = author.AuthorId,
            Text = Text,
            TimeStamp = DateTime.Now,
            Author = author
        };
        
        await CheepRepository.SaveCheep(cheep, author);
        
        return RedirectToPage();
    }
    
    /// <summary>
    /// Allows the logged-in user to follow another user.
    /// </summary>
    /// <param name="followAuthorName">The name of the author to follow.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostFollow(string followAuthorName)
    {
        //Finds the author thats logged in
        var authorName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }
        
        var author = await AuthorRepository.FindAuthorWithEmail(authorName);
        
        //Finds the author that the logged in author wants to follow
        var followAuthor = await AuthorRepository.FindAuthorWithName(followAuthorName);
        
        await AuthorRepository.FollowUserAsync(author.AuthorId, followAuthor.AuthorId);
        
        //updates the current author's list of followed authors
        FollowedAuthors = await AuthorRepository.GetFollowing(author.AuthorId);
        
        return RedirectToPage();
    }
    
    /// <summary>
    /// Allows the logged-in user to unfollow another user.
    /// </summary>
    /// <param name="followAuthorName">The name of the author to unfollow.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostUnfollow(string followAuthorName)
    {
        //Finds the author thats logged in
        var authorName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }
        
        var author = await AuthorRepository.FindAuthorWithEmail(authorName);
        
        //Finds the author that the logged in author wants to follow
        var followAuthor = await AuthorRepository.FindAuthorWithName(followAuthorName);
        
        await AuthorRepository.UnFollowUserAsync(author.AuthorId, followAuthor.AuthorId);
        
        //updates the current author's list of followed authors
        FollowedAuthors = await AuthorRepository.GetFollowing(author.AuthorId);
        
        return RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to like a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The author of the cheep to like.</param>
    /// <param name="text">The text of the cheep to like.</param>
    /// <param name="timeStamp">The timestamp of the cheep to like.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostLike(string cheepAuthorName, string text, string timeStamp)
    {
        // Find the author that's logged in
        var authorName = User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await AuthorRepository.FindAuthorWithName(authorName);
        var cheep = await CheepRepository.FindCheep(text,timeStamp, cheepAuthorName);
        
        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }
        
        // Adds the cheep to the author's list of liked cheeps
        await CheepRepository.LikeCheep(cheep, author);
        
        LikedCheeps = await AuthorRepository.GetLikedCheeps(author.AuthorId);
        
        return RedirectToPage();
    }

    /// <summary>
    /// Allows the logged-in user to unlike a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The author of the cheep to unlike.</param>
    /// <param name="text">The text of the cheep to unlike.</param>
    /// <param name="timeStamp">The timestamp of the cheep to unlike.</param>
    /// <returns>An <see cref="ActionResult"/> redirecting to the current page after the operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<ActionResult> OnPostUnLike(string cheepAuthorName, string text, string timeStamp)
    {
        // Find the author that's logged in
        var authorName = User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await AuthorRepository.FindAuthorWithName(authorName);
        var cheep = await CheepRepository.FindCheep(text,timeStamp,cheepAuthorName);
        
        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }
        
        await CheepRepository.UnLikeCheep(cheep, author);
        
        LikedCheeps = await AuthorRepository.GetLikedCheeps(author.AuthorId);
        
        return RedirectToPage();
    }

    
    /// <summary>
    /// Determines whether the logged-in user has liked a specific cheep.
    /// </summary>
    /// <param name="cheepAuthorName">The author of the cheep.</param>
    /// <param name="text">The text of the cheep.</param>
    /// <param name="timeStamp">The timestamp of the cheep.</param>
    /// <returns>A <see cref="bool"/> indicating whether the cheep is liked by the user.</returns>
    /// <exception cref="ArgumentException">Thrown if the cheep or the logged-in user's name is null or empty.</exception>
    public async Task<bool> DoesUserLikeCheep(string cheepAuthorName, string text, string timeStamp)
    {
        var authorName = User.FindFirst("Name")?.Value;
        if (string.IsNullOrEmpty(authorName))
        {
            throw new ArgumentException("Author name cannot be null or empty.");
        }

        var author = await AuthorRepository.FindAuthorWithName(authorName);
        var cheep = await CheepRepository.FindCheep(text,timeStamp,cheepAuthorName);
        
        if (cheep == null)
        {
            throw new ArgumentException("Cheep could not be found.");
        }
        
        return await CheepRepository.DoesUserLikeCheep(cheep, author);
    }
}

