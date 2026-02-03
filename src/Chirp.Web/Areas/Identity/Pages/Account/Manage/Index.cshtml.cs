// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
{
    private readonly UserManager<Author> _userManager;
    private readonly SignInManager<Author> _signInManager;
    private readonly CheepDBContext _context;
    private readonly ICheepRepository _cheepRepository;

    public IndexModel(UserManager<Author> userManager, SignInManager<Author> signInManager, CheepDBContext context, ICheepRepository cheepRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _cheepRepository = cheepRepository;
        
    }

    public string Email { get; set; }

    public string Username { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Display(Name = "NewUserName")]
        public string NewUserName { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }

    private async Task LoadAsync(Author user)
    {
        Email = await _userManager.GetEmailAsync(user); // Retrieve email
        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        Username = user.Name; // Set current username

        Input = new InputModel
        {
            NewUserName = Username, // Pre-fill with the current username
            PhoneNumber = phoneNumber
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }
        
        if (user.PhoneNumber != Input.PhoneNumber)
        {
            var existingClaim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "PhoneNumber");
            if (existingClaim != null)
            {
                //Removes the claim if the claim exists
                var removeResult = await _userManager.RemoveClaimAsync(user, existingClaim);
                if (!removeResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to remove existing phone number claim.";
                    return RedirectToPage();
                }
            }
            
            //Creates a new claim with the new username.
            var newClaim = new Claim("PhoneNumber", Input.PhoneNumber);
            // Adds the claim to database
            var addClaimResult = await _userManager.AddClaimAsync(user, newClaim);
            if (!addClaimResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to add new phone number claim.";
                return RedirectToPage();
            }
            
            //This updates the users (authors) name, which also makes sure that the cheeps have the NewUserName
            user.PhoneNumber = Input.PhoneNumber; 
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                StatusMessage = "Unexpected error when trying to update phone number.";
                return RedirectToPage();
            }
            
        }
        
        if (user.Name != Input.NewUserName)
        {
            
            //Gets the existing claim (Which is made in Register when a new user is created).
            var existingClaim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "Name");
            if (existingClaim != null)
            {
                //Removes the claim if the claim exists
                var removeResult = await _userManager.RemoveClaimAsync(user, existingClaim);
                if (!removeResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to remove existing name claim.";
                    return RedirectToPage();
                }
            }
            
            //Creates a new claim with the new username.
            var newClaim = new Claim("Name", Input.NewUserName);
            // Adds the claim to database
            var addClaimResult = await _userManager.AddClaimAsync(user, newClaim);
            if (!addClaimResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to add new name claim.";
                return RedirectToPage();
            }
            
            //This updates the users (authors) name, which also makes sure that the cheeps have the NewUserName
            user.Name = Input.NewUserName; 
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                StatusMessage = "Unexpected error when trying to update name.";
                return RedirectToPage();
            }
        }
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

}
}
