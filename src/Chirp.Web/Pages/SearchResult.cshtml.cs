using Chirp.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chirp.Infrastructure;

namespace Chirp.Web.Pages
{
    /// <summary>
    /// This class handles the functionality of the searchbar for finding authors.
    /// </summary>
    public class SearchResultsModel : PageModel
    {
        private readonly IAuthorRepository _authorRepository;
        [BindProperty(SupportsGet = true)]
        public string? SearchWord { get; set; }
        public List<AuthorDTO> AuthorDTOs { get; set; } = new List<AuthorDTO>();

        
        public SearchResultsModel(IAuthorRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }
        
        /// <summary>
        /// Handles the GET request to search authors based on the search input.
        /// </summary>
        public async Task OnGet()
        {
            if (!string.IsNullOrEmpty(SearchWord))
            {
                AuthorDTOs = await _authorRepository.SearchAuthorsAsync(SearchWord);
                
            }
        }
    }
}