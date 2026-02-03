using Chirp.Core;
using Chirp.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Chirp.Infrastructure.Test;

public class UnitTestChirpInfrastructure : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private readonly ITestOutputHelper _output;
    
    public UnitTestChirpInfrastructure(ITestOutputHelper output)
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


/*
    [Fact]
    public async Task UnitTestGetNonexistingAuthor()
    {
        await using var dbContext = CreateContext();
        var _cheepRepository = new CheepRepository(new DBFacade(dbContext), dbContext);

        var author = await _cheepRepository.FindAuthorWithName("DrDontExist");

        Assert.Null(author);
    }
    */
    
    [Fact]
    public async Task UnitTestDuplicateAuthors()
    {
        await using var dbContext = CreateContext();
        
        var testAuthor1 = new Author
        {
            Name = "Test Name",
            Email = "test@gmail.com",
            Cheeps = new List<Cheep>(),
        };
        
        await dbContext.Authors.AddAsync(testAuthor1);
        await dbContext.SaveChangesAsync(); 

        var testAuthor2 = new Author
        {
            Name = "Test Name", 
            Email = "test@gmail.com", 
            Cheeps = new List<Cheep>(),
        };
        
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await dbContext.Authors.AddAsync(testAuthor2);
            await dbContext.SaveChangesAsync(); 
        });
    }

    [Fact]
    public async Task UnitTestNoAuthorNameDuplicates()
    {
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);
        
        var testAuthor1 = new Author
        {
            Name = "Jacqualine Gilcoine",
            Email = "test@gmail.com",
            Cheeps = new List<Cheep>(),
        };
        
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await dbContext.Authors.AddAsync(testAuthor1);
            await dbContext.SaveChangesAsync(); 
        });
    }

    [Fact]
    public async Task UnitTestNoEmailDuplicates()
    {
        await using var dbContext = CreateContext();
        DbInitializer.SeedDatabase(dbContext);

        var testAuthor1 = new Author
        {
            Name = "Jacqie Gilcoine",
            Email = "Jacqualine.Gilcoine@gmail.com",
            Cheeps = new List<Cheep>(),
        };
        
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await dbContext.Authors.AddAsync(testAuthor1);
            await dbContext.SaveChangesAsync(); 
        });
    }
  
    
}
