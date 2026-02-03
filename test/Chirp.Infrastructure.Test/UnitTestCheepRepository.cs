using Chirp.Core;
using Chirp.Web;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Chirp.Infrastructure.Test;

public class UnitTestCheepRepository : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private readonly ITestOutputHelper _output;
    
    public UnitTestCheepRepository(ITestOutputHelper output)
    {
        _output = output; // Assigning the output to the private field
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }

    private CheepDBContext CreateContext()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection is null.");
        }
    
        var options = new DbContextOptionsBuilder<CheepDBContext>()
            .UseSqlite(_connection) 
            .Options;
    
        var context = new CheepDBContext(options);
        context.Database.EnsureCreated(); 
        return context;
    }
    
    [Fact]
    public async Task UnitTestTestPageSize()
    {
        //Arrange
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository( dbContext);
        
        //Act
        List<CheepDTO> cheeps = await cheepRepository.GetCheeps(1, 32);
        List<CheepDTO> cheeps2 = await cheepRepository.GetCheeps(1, 12);
        
        _output.WriteLine("cheeps: {0}, cheeps2: {1}", cheeps.Count, cheeps2.Count);

        //Assert
        Assert.Equal(32, cheeps.Count);
        Assert.Equal(12, cheeps2.Count);
    }
    
    [Fact]
    public async Task UnitTestTestNoCheepsOnEmptyPage()
    {
        //Arrange
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);
        
        //Act
        List<CheepDTO> cheeps = await cheepRepository.GetCheeps(100000, 32);
        
        _output.WriteLine("cheeps: {0}", cheeps.Count);

        //Assert
        Assert.Empty(cheeps);
    }
    
    
    [Fact]
    public async Task UnitTestGetCheepsFromAuthor()
    {
        //Arrange
        await using var dbContext = CreateContext();
        var cheepRepository = new CheepRepository(dbContext);

        List<Cheep> cheeps = new List<Cheep>();
        
        var testAuthor1 = new Author
        {
            AuthorId = 1,
            Name = "Nicola Schwarowski",
            Email = "test@gmail.com",
            Cheeps = new List<Cheep>(),
        };

        dbContext.Authors.Add(testAuthor1);

        var testCheep = new Cheep
        {
            Text = "Hello, my name is Nicola!",
            AuthorId = testAuthor1.AuthorId, 
            Author = testAuthor1,
        };
        
        testAuthor1.Cheeps.Add(testCheep);
        await cheepRepository.SaveCheep(testCheep, testAuthor1);
        await dbContext.SaveChangesAsync();
        
        //Act
        cheeps = await cheepRepository.GetCheepsByAuthor(testAuthor1.AuthorId);
        await dbContext.SaveChangesAsync();
        
        //Assert
        foreach (Cheep cheep in cheeps)
        {
            _output.WriteLine("cheep Author: {0} and cheeps written: {1}", cheep.Author.Name, cheeps.Count());
            Assert.Equal(testAuthor1.Name, cheep.Author.Name);
            Assert.Equal("Hello, my name is Nicola!", cheep.Text);
        }
        Assert.Single(cheeps);
    }
    
    
    [Fact]
    public async Task UnitTestSavesCheepAndLoadsAuthorCheeps()
    {
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);
        var authorRepository = new AuthorRepository(dbContext);
        
        List<CheepDTO> cheeps = new List<CheepDTO>();
        string authorName = "Jacqualine Gilcoine";
        Author author = await authorRepository.FindAuthorWithName(authorName);
        int authorId = author.AuthorId;

        var cheep = new Cheep
        {
            AuthorId = authorId,
            Text = "Hello, I am from France",
        };

        // Act
        await cheepRepository.SaveCheep(cheep, author);
        await dbContext.SaveChangesAsync();
        
        var savedCheep = await dbContext.Cheeps.FindAsync(cheep.CheepId);
        _output.WriteLine("cheep {0}", cheep.CheepId);
        
        Assert.NotNull(savedCheep);
        Assert.Equal("Hello, I am from France", savedCheep.Text);
        Assert.Equal(author.AuthorId, savedCheep.AuthorId);

        // Check that the author's cheeps collection is loaded
        var updatedAuthor = await dbContext.Authors.FindAsync(author.AuthorId);
        Assert.NotNull(updatedAuthor);
        Assert.NotNull(updatedAuthor.Cheeps);
        Assert.Contains(updatedAuthor.Cheeps, c => c.Text == "Hello, I am from France");
    }
    
    
    [Fact]
    public async Task UnitTestGetCheepsShouldReturnCheepsWithAuthorName()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);
            
        // Seed test data
        var author = new Author { Name = "TestAuthor" };
        var cheep = new Cheep { Author = author, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        dbContext.Authors.Add(author);
        dbContext.Cheeps.Add(cheep);
        await dbContext.SaveChangesAsync();
            
        // Act
        var cheeps = await cheepRepository.GetCheeps(1, 10);
            
        // Assert
        var retrievedCheep = cheeps.First();
        Assert.Equal("TestAuthor", retrievedCheep.AuthorName);
        Assert.Equal("Hello World!", retrievedCheep.Text);
    }

    [Fact]
    public async Task UnitTestSaveCheepThrowsExceptionWhenAuthorsCheepsIsNull()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        var author = new Author
        {
            Name = "TestAuthor",
            Cheeps = null // Set Cheeps to null to test this scenario
        };

        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync();

        // Associate the Cheep with the Author
        var cheep = new Cheep
        {
            Text = "Test Cheep",
            TimeStamp = DateTime.UtcNow,
            AuthorId = author.Id // This ensures the foreign key is valid
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cheepRepository.SaveCheep(cheep, author)
        );

        Assert.Equal("Author's Cheeps collection is null.", exception.Message);
        Assert.Null(author.Cheeps);
    }

    [Fact]
    public async Task UnitTestDoesUserLikeCheep()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        var author1 = new Author { Name = "TestAuthor1" };
        var cheep1 = new Cheep { Author = author1, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        var cheep2 = new Cheep { Author = author1, Text = "You will never catch me!", TimeStamp = DateTime.UtcNow };
        
        var author2 = new Author { Name = "TestAuthor2" };
        
        dbContext.Authors.Add(author1);
        dbContext.Authors.Add(author2);
        dbContext.Cheeps.Add(cheep1);
        dbContext.Cheeps.Add(cheep2);
        
        await dbContext.SaveChangesAsync();

        //Act & Assert
        await cheepRepository.LikeCheep(cheep1, author2);
        
        Assert.True(await cheepRepository.DoesUserLikeCheep(cheep1, author2));
        Assert.False(await cheepRepository.DoesUserLikeCheep(cheep2, author2));
    }
    
    [Fact]
    public async Task UnitTestDoesUserLikeCheepReturnFalseIfLikedCheepsIsNull()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        var author1 = new Author { Name = "TestAuthor1" };
        var cheep = new Cheep { Author = author1, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        
        var author2 = new Author { Name = "TestAuthor2" };
        
        dbContext.Authors.Add(author1);
        dbContext.Authors.Add(author2);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();

        //Act & Assert
        author2.LikedCheeps = null;
        
        Assert.False(await cheepRepository.DoesUserLikeCheep(cheep, author2));
    }

    [Fact]
    public async Task UnitTestFindCheepShouldReturnCheep()
    {
        // Arrange
        await using var dbContext = CreateContext();
        
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        string text = "Hello World!";
        var dateTimeTimeStamp = DateTime.UtcNow;
        string timeStamp = DateTime.TryParse(dateTimeTimeStamp.ToString(), out dateTimeTimeStamp) ? dateTimeTimeStamp.ToString() : dateTimeTimeStamp.ToString();
        string name = "TestAuthor";
        
        var author = new Author { Name = name , Cheeps = new List<Cheep>(), AuthorId = 1, Id = 1};
        var cheep = new Cheep { Author = author, Text = text, TimeStamp = dateTimeTimeStamp };
        
        dbContext.Authors.Add(author);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();
        
        //Act & Assert
        Assert.Same(cheep, await cheepRepository.FindCheep(text,timeStamp,name));
    }
    
    [Fact]
    public async Task UnitTestFindCheepShouldReturnNull()
    {
        // Arrange
        await using var dbContext = CreateContext();
        
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        string text1 = "Hello World!";
        string text2 = "Goodbye World!";
        var dateTimeTimeStamp = DateTime.UtcNow;
        string timeStamp = DateTime.TryParse(dateTimeTimeStamp.ToString(), out dateTimeTimeStamp) ? dateTimeTimeStamp.ToString() : dateTimeTimeStamp.ToString();
        string name = "TestAuthor";
        
        var author = new Author { Name = name , Cheeps = new List<Cheep>(), AuthorId = 1, Id = 1};
        var cheep = new Cheep { Author = author, Text = text1, TimeStamp = dateTimeTimeStamp };
        
        dbContext.Authors.Add(author);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();
        
        //Act & Assert
        Assert.Null(await cheepRepository.FindCheep(text2,timeStamp,name));
    }
    
    [Fact]
    public async Task UnitTestFindCheepShouldRaiseExceptionIfDateTimeFormatIsInvalid()
    {
        // Arrange
        await using var dbContext = CreateContext();
        
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data and make a time stamp with a wrong format to raise an exception
        string text = "Hello World!";
        string timeStamp = "02-04---56.";
        string name = "TestAuthor";
        
        
        await dbContext.SaveChangesAsync();
        
        //Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await cheepRepository.FindCheep(text, timeStamp, name);
        });
    }
    
    [Fact]
    public async Task UnitTestLikeCheepShouldAddCheepToLikedCheepsAndIncrementLikesCount()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        var author1 = new Author { Name = "TestAuthor1" };
        var cheep = new Cheep { Author = author1, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        cheep.Likes = 0;
        
        var author2 = new Author { Name = "TestAuthor2" };
        author2.LikedCheeps = new List<Cheep> { cheep };
        
        dbContext.Authors.Add(author1);
        dbContext.Authors.Add(author2);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();

        //Act & Assert
        await cheepRepository.LikeCheep(cheep, author2);

        Assert.Contains(cheep, author2.LikedCheeps);
        Assert.Equal(1, cheep.Likes);
    }
    
    [Fact]
    public async Task UnitTestLikeCheepShouldNotAddCheepToLikedCheeps()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        var author1 = new Author { Name = "TestAuthor1" };
        var cheep = new Cheep { Author = author1, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        cheep.Likes = 0;
        
        var author2 = new Author { Name = "TestAuthor2" };
        
        //Make LikedCheeps null to make the LikeCheep method return null
        author2.LikedCheeps = null;
        
        dbContext.Authors.Add(author1);
        dbContext.Authors.Add(author2);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();

        //Act & Assert
        await cheepRepository.LikeCheep(cheep, author2);

        Assert.Null(author2.LikedCheeps);
        Assert.Equal(0, cheep.Likes);
    }
    
    [Fact]
    public async Task UnitTestUnLikeCheepShouldRemoveCheepFromLikedCheepsAndDecrementLikesCount()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        var author1 = new Author { Name = "TestAuthor1" };
        var cheep = new Cheep { Author = author1, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        cheep.Likes = 0;
        
        var author2 = new Author { Name = "TestAuthor2" };
        
        //Make LikedCheeps null to make the LikeCheep method return null
        author2.LikedCheeps = new List<Cheep>();
        
        dbContext.Authors.Add(author1);
        dbContext.Authors.Add(author2);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();

        //Act & Assert
        await cheepRepository.LikeCheep(cheep, author2);
        await cheepRepository.UnLikeCheep(cheep, author2);
        
        Assert.DoesNotContain(cheep, author2.LikedCheeps);
        Assert.Equal(0, cheep.Likes);
    }
    
    [Fact]
    public async Task UnitTestUnLikeCheepShouldNotDecrementLikesCount()
    {
        // Arrange
        await using var dbContext = CreateContext();
        //DbInitializer.SeedDatabase(dbContext);
        var cheepRepository = new CheepRepository(dbContext);

        // Seed test data
        var author1 = new Author { Name = "TestAuthor1" };
        var cheep = new Cheep { Author = author1, Text = "Hello World!", TimeStamp = DateTime.UtcNow };
        cheep.Likes = 0;
        
        var author2 = new Author { Name = "TestAuthor2" };
        
        //Make LikedCheeps null to make the LikeCheep method return null
        author2.LikedCheeps = new List<Cheep>();
        
        dbContext.Authors.Add(author1);
        dbContext.Authors.Add(author2);
        dbContext.Cheeps.Add(cheep);
        
        await dbContext.SaveChangesAsync();

        //Act & Assert
        await cheepRepository.LikeCheep(cheep, author2);
        
        author2.LikedCheeps = null;
        
        await cheepRepository.UnLikeCheep(cheep, author2);

        //Since LikedCheeps is null we can only check that Likes has not been decremented
        Assert.Null(author2.LikedCheeps);
        Assert.Equal(1, cheep.Likes);
    }
}