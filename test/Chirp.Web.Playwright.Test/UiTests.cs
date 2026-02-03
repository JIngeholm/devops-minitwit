using Microsoft.Playwright;
using Xunit;

namespace Chirp.Web.Playwright.Test;

[TestFixture, NonParallelizable]
public class UiTests : PageTest, IClassFixture<CustomTestWebApplicationFactory>, IDisposable
{
    private IBrowserContext? _context;
    private IBrowser? _browser;
    private CustomTestWebApplicationFactory _factory;
    private string _serverAddress;
    private IPlaywright _playwright;
    private IPage _page = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new CustomTestWebApplicationFactory();
        _serverAddress = _factory.ServerAddress;
        
        await InitializeBrowserAndCreateBrowserContextAsync();
            
        var test = TestContext.CurrentContext.Test;

        // Check if the test is marked with the "SkipSetUp" category
        if (!test.Properties["Category"].Contains("SkipSetUp"))
        {
            await SetUpRegisterAndLogin();
        }
    }
    
    [TearDown] 
    public void TearDown()
    {
        Dispose();
    }
    
    [Test, Category("SkipSetUp")] 
    public async Task UsersCanRegister()
    {
        _page = await _context!.NewPageAsync();
        await _page.GotoAsync(_serverAddress);
        
        await _page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await _page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));

        //Username
        var usernameInput = _page.GetByLabel("Username");
        await usernameInput.ClickAsync();
        await Expect(usernameInput).ToBeFocusedAsync();
        await usernameInput.FillAsync("Cecilie");
        await Expect(usernameInput).ToHaveValueAsync("Cecilie");
        await _page.GetByLabel("Username").PressAsync("Tab");
        
        //Email
        var emailInput = _page.GetByPlaceholder("name@example.com");
        await emailInput.FillAsync("ceel@itu.dk");
        await Expect(emailInput).ToHaveValueAsync("ceel@itu.dk");
        
        //password
        //var passwordInput = _page.GetByRole(AriaRole.Textbox, new() { NameString = "Password" });
        var passwordInput = _page.Locator("input[id='Input_Password']");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync("Johan1234!");
        await Expect(passwordInput).ToHaveValueAsync("Johan1234!");
        await passwordInput.PressAsync("Tab");
        await Expect(passwordInput).Not.ToBeFocusedAsync();

        //var confirmPassword = _page.GetByLabel("Confirm Password");
        var confirmPassword = _page.Locator("input[id='Input_ConfirmPassword']");
        await confirmPassword.FillAsync("Johan1234!");
        await Expect(confirmPassword).ToHaveValueAsync("Johan1234!");
        
        //click on register button
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Register" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(_serverAddress);
    }
    
    [Test, Category("SkipSetUp")]
    public async Task UserCanRegisterAndLogin()
    {
        //go to base server address
        _page = await _context!.NewPageAsync();
        await _page.GotoAsync(_serverAddress);
        
        //first register user, because a new in memory database is created for each test. 
        await _page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync(); 
        await _page.WaitForURLAsync(new Regex("/Identity/Account/Register$")); 
        await _page.GetByLabel("Username").ClickAsync(); 
        await _page.GetByLabel("Username").FillAsync("Cecilie"); 
        await _page.GetByLabel("Username").PressAsync("Tab"); 
        await _page.GetByPlaceholder("name@example.com").FillAsync("ceel@itu.dk");
        await _page.Locator("input[id='Input_Password']").ClickAsync();
        await _page.Locator("input[id='Input_Password']").FillAsync("Cecilie1234!"); 
        await _page.Locator("input[id='Input_Password']").PressAsync("Tab"); 
        await _page.Locator("input[id='Input_ConfirmPassword']").FillAsync("Cecilie1234!"); 
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Register" }).ClickAsync(); 
        
        await Expect(_page).ToHaveURLAsync(_serverAddress);
        var loggedIn = _page.GetByText("What's on your mind");
        await Expect(loggedIn).ToBeVisibleAsync();
    }
    
    [Test]
    public async Task UserCanShareCheepFromPublicTimeline()
    {
        //send cheep   
        var cheepTextField = _page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my group is the best group");
        await Expect(cheepTextField).ToHaveValueAsync("Hello, my group is the best group");
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Share" }).ClickAsync();
        //check if there is a cheep with that text on the page after share button has been clicked. 
        var cheep = _page.GetByText("Hello, my group is the best group");
        await cheep.HighlightAsync();
        
        await Expect(cheep).ToBeVisibleAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress));
    }
    
    [Test]
    public async Task UserCanShareCheepFromUserTimeline()
    {
        await _page.GetByRole(AriaRole.Link, new() { NameString = "my timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Cecilie"));
        
        //send cheep   
        var cheepTextField = _page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my name is Cecilie");
        await Expect(cheepTextField).ToHaveValueAsync("Hello, my name is Cecilie");
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Share" }).ClickAsync();
        //check if there is a cheep with that text on the page after share button has been clicked. 
        var cheep = _page.GetByText("Hello, my name is Cecilie");
        await cheep.HighlightAsync();
        
        await Expect(cheep).ToBeVisibleAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Cecilie"));
    }
    
    [Test]
    public async Task UserCanGoToMyTimelineByClickingOnMyTimeline()
    {
        await _page.GetByRole(AriaRole.Link, new() { NameString = "my timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Cecilie"));
    }
    
    [Test]
    public async Task UserCanGoToPublicTimeline()
    {
        await _page.GetByRole(AriaRole.Link, new() { NameString = "public timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress));
    }
    
    [Test]
    public async Task UserCanChangeAccountInformation()
    {
        //go to account
        await _page.GetByRole(AriaRole.Link, new() { NameString = "Account" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage"));
        
        //change username 
        var usernameField = _page.GetByPlaceholder("Username"); 
        await usernameField.ClickAsync();
        await usernameField.FillAsync("JohanIngeholm");
        await Expect(usernameField).ToHaveValueAsync("JohanIngeholm");
        
        //enter phonenumber
        var phonenumberField = _page.GetByPlaceholder("Please enter your phone number.");
        await phonenumberField.ClickAsync();
        await phonenumberField.FillAsync("31690155");
        await Expect(phonenumberField).ToHaveValueAsync("31690155");
        
        //save changes
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Save" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage"));
        
        //text with changes has been saved is visible on screen to illustrate save button has been pressed.
        var textSavings = _page.GetByText("Your profile has been updated");
        await textSavings.ClickAsync();
        await Expect(_page.Locator("text=Your profile has been updated")).ToBeVisibleAsync();
    }
    
    [Test]
    public async Task UserCanChangeEmail()
    {
        //go to account
        await _page.GetByRole(AriaRole.Link, new() { NameString = "Account" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage"));
        
        //go to email in account
        await _page.GetByRole(AriaRole.Link, new() { NameString = "Email" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage/Email"));
        
        //enter new email
        var emailField = _page.GetByPlaceholder("Please enter new email");
        await emailField.ClickAsync();
        await emailField.FillAsync("jing@itu.dk");
        await Expect(emailField).ToHaveValueAsync("jing@itu.dk");
        
        //change email button
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Change email" }).ClickAsync();
        
        await _page.GetByRole(AriaRole.Link, new() { NameString = "Account" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage"));
        
        var emailFieldInAccount = _page.GetByPlaceholder("Email");
        await Expect(emailFieldInAccount).ToHaveValueAsync("jing@itu.dk");
        
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage"));
    }
    
    [Test]
    public async Task UserCanLogOut()
    {
        await _page.GetByRole(AriaRole.Link, new() { NameString = "public timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(_serverAddress);
        
        //user can log out
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Logout" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Logout"));
    }

    [Test]
    public async Task FollowAndUnfollowOnPublicTimeline()
    {
        //find the follow-button for a specific cheep
        var followButton = _page.Locator("li").Filter(new() 
        { 
            HasText = "Coffee House now is what we hear the worst." 
        }).GetByRole(AriaRole.Button, new() { NameString = "Follow" });

        //follow author
        await Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Unfollow");

        //unfollow author
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Follow");
    }
    
    [Test]
    public async Task GoToAnotherUsersTimelineAndFollowAndUnfollow()
    {
        // go to another user's timeline 
        var userTimelinePage = _page.GetByRole(AriaRole.Link, new() { Name = "Jacqualine Gilcoine" }).First;
        
        await userTimelinePage.ClickAsync();
        await Expect (_page).ToHaveURLAsync(new Regex(_serverAddress + $"Jacqualine"));
        
        //find the follow-button for a specific cheep
        var followButton = _page.Locator("li").Filter(new() 
        { 
            HasText = "Coffee House now is what we hear the worst." 
        }).GetByRole(AriaRole.Button, new() { NameString = "Follow" });

        //follow author
        await Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Unfollow");

        //unfollow author
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Follow");
    }
    
    [Test]
    public async Task GoToAnotherUsersTimelineAndSeeFirst32CheepsWrittenByThatAuthor()
    {
        // go to another user's timeline 
        var userTimelinePage = _page.GetByRole(AriaRole.Link, new() { Name = "Jacqualine Gilcoine" }).First;

        await userTimelinePage.ClickAsync();
        await Expect (_page).ToHaveURLAsync(new Regex(_serverAddress + $"Jacqualine"));
        
        var cheeps = _page.Locator("li").Filter(new()
        {
            HasText = "Jacqualine Gilcoine"
        }).GetByRole(AriaRole.Link);
        
        // Assert that there are exactly 32 elements
        await Expect(cheeps).ToHaveCountAsync(32);
    }
    
    [Test]
    public async Task CheckCharCountOnWritingCheeps()
    {
        //go to my timeline
        await _page.GetByRole(AriaRole.Link, new() { Name = "My timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Cecilie"));
        
        //click on text field to write cheep and write a cheep. 
        var cheepTextField = _page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my name is Cecilie");
        await Expect(cheepTextField).ToHaveValueAsync("Hello, my name is Cecilie");
        
        //see charcount label increase to 25
        var charCountSpan = _page.Locator("span[id='charCount']");
        await Expect(charCountSpan).ToHaveTextAsync("25/160");
    }
    
    [Test]
    public async Task SearchForUserAndFollow()
    {
        //search for author
        var searchField = _page.GetByPlaceholder("Search authors...");
        await searchField.ClickAsync();
        await searchField.FillAsync("Mellie");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
        
        //show search results and click on user
        await Expect (_page).ToHaveURLAsync(new Regex(_serverAddress + $"SearchResults"));
        await _page.GetByRole(AriaRole.Link, new() { Name = "Mellie Yost" }).ClickAsync();
        await Expect (_page).ToHaveURLAsync(new Regex(_serverAddress + $"Mellie"));

        var followButton = _page.Locator("li").Filter(new() 
        { 
            HasText = "But what was behind the barricade" 
        }).GetByRole(AriaRole.Button, new() { NameString = "Follow" });

        //follow author
        await Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Unfollow");
    }
    
    [Test]
    public async Task UnfollowOnUserTimeline()
    {
        //find the follow-button for a specific cheep
        var followButton = _page.Locator("li").Filter(new() 
        { 
            HasText = "Coffee House now is what we hear the worst." 
        }).GetByRole(AriaRole.Button, new() { NameString = "Follow" });

        //follow author
        await Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Unfollow");
        
        //go to my timeline
        await _page.GetByRole(AriaRole.Link, new() { NameString = "my timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Cecilie"));
        
        //locate cheep from the author we just followed
        await Expect(_page.Locator("li:has-text('Coffee House now is what we hear the worst.')")).ToBeVisibleAsync();
        await Expect(followButton).ToHaveTextAsync("Unfollow");
        
        //unfollow author
        await followButton.ClickAsync();
        await Expect(followButton).ToBeHiddenAsync();
        await Expect(_page.Locator("text=There are no cheeps so far.")).ToBeVisibleAsync();
        
        //go back to public timeline to check the unfollow-button has changed back to follow
        await _page.GetByRole(AriaRole.Link, new() { NameString = "public timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(_serverAddress);
        await Expect(followButton).ToHaveTextAsync("Follow");
    }
    
    [Test]
    public async Task GoToNextPageWithButton()
    {
        var nextButton = _page.GetByRole(AriaRole.Link, new() { Name = "Next" });
        await nextButton.ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex($"{_serverAddress}\\?page=2"));
        
        var previousButton = _page.GetByRole(AriaRole.Link, new() { Name = "Previous" });
        await Expect(previousButton).ToBeVisibleAsync();
    }
    
    [Test]
    public async Task UserCanDeleteTheirAccount()
    {
        //go to about me page
        await _page.GetByRole(AriaRole.Link, new() { Name = "About Me" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage/PersonalData"));
        
        //click forget me
        await _page.GetByRole(AriaRole.Link, new() { Name = "Forget me" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Identity/Account/Manage/DeletePersonalData"));
        
        //confirm delete data and close account
        var passwordInput = _page.GetByPlaceholder("Please enter your password");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync("Cecilie1234!");
        await Expect(passwordInput).ToHaveValueAsync("Cecilie1234!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Delete data and close my" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(_serverAddress);
    }
    
    [Test]
    public async Task UserCanLikeAndUnlikeOtherCheepsOnPublicTimeline()
    {
        var likeButton = _page.Locator("li").Filter(new()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-"
        }).Locator("button.like-button-not-liked");
        
        var likeCount = _page.Locator("li").Filter(new()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-"
        }).Locator(".like-button-container span").Nth(1);
        
        var unLikeButton = _page.Locator("li").Filter(new()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-"
        }).Locator("button.like-button-liked");
        
        await Expect(likeCount).ToHaveTextAsync("0");
        await likeButton.ClickAsync();
        await Expect(likeCount).ToHaveTextAsync("1");
        await unLikeButton.ClickAsync();
        await Expect(likeCount).ToHaveTextAsync("0");
    }
    [Test]
    public async Task UserCanFollowAndLikeOtherCheepsOnMyTimeline()
    {
        //find the follow-button for a specific cheep
        var followButton = _page.Locator("li").Filter(new() 
        { 
            HasText = "Coffee House now is what we hear the worst." 
        }).GetByRole(AriaRole.Button, new() { NameString = "Follow" });

        //follow author
        await Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await Expect(followButton).ToHaveTextAsync("Unfollow");
        
        //go to my timeline
        await _page.GetByRole(AriaRole.Link, new() { Name = "My timeline" }).ClickAsync();
        await Expect(_page).ToHaveURLAsync(new Regex(_serverAddress + $"Cecilie"));
        
        var likeButton = _page.Locator("li").Filter(new()
        {
            HasText = "Jacqualine Gilcoine Unfollow Once, I remember, to be a rock, but it is this"
        }).GetByRole(AriaRole.Button).Nth(1);

        var likeCount0 = _page.GetByText("0", new() { Exact = true }).Nth(3);
        var likeCount1 = _page.GetByText("1", new() { Exact = true });
        
        var unLikeButton = _page.Locator("li").Filter(new()
        {
            HasText = "Jacqualine Gilcoine Unfollow Once, I remember, to be a rock, but it is this"
        }).GetByRole(AriaRole.Button).Nth(1);

        await Expect(likeCount0).ToHaveTextAsync("0");
        await likeButton.ClickAsync();
        await Expect(likeCount1).ToHaveTextAsync("1");
        await unLikeButton.ClickAsync();
        await Expect(likeCount0).ToHaveTextAsync("0");
    }
    
    private async Task SetUpRegisterAndLogin()
    { 
        _page = await _context!.NewPageAsync(); 
        await _page.GotoAsync(_serverAddress);
        //first register user, because a new in memory database is created for each test.
        await _page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await _page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));
        await _page.GetByLabel("Username").ClickAsync(); 
        await _page.GetByLabel("Username").FillAsync("Cecilie"); 
        await _page.GetByLabel("Username").PressAsync("Tab"); 
        await _page.GetByPlaceholder("name@example.com").FillAsync("ceel@itu.dk");
        await _page.Locator("input[id='Input_Password']").ClickAsync();
        await _page.Locator("input[id='Input_Password']").FillAsync("Cecilie1234!"); 
        await _page.Locator("input[id='Input_Password']").PressAsync("Tab"); 
        await _page.Locator("input[id='Input_ConfirmPassword']").FillAsync("Cecilie1234!");
        await _page.GetByRole(AriaRole.Button, new() { NameString = "Register" }).ClickAsync();
        await _page.GetByText("What's on your mind Cecilie?").WaitForAsync();
    }

    private async Task InitializeBrowserAndCreateBrowserContextAsync() 
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, //Set to false if you want to see the browser
        });
            
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions());
    }
    
    //dispose browser and context after each test
    public void Dispose()
    {
       _context?.DisposeAsync().GetAwaiter().GetResult();
       _browser?.DisposeAsync().GetAwaiter().GetResult();
    }
}