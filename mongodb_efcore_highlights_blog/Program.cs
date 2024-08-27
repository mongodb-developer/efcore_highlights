using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Conventions;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");

if (connectionString == null)
{
    Console.WriteLine("You must set your 'MONGODB_URI' environment variable. To learn how to set it, see https://www.mongodb.com/docs/drivers/csharp/current/quick-start/#set-your-connection-string");
    Environment.Exit(0);
}

var client = new MongoClient(connectionString);
var database = client.GetDatabase("sample_mflix");
var db = MflixDbContext.Create(database);

var movie = db.Movies.First(m => m.Title == "Back to the Future");
Console.WriteLine(movie.Plot);

movie.AdaptedFromBook = false;
await db.SaveChangesAsync();


var moviesCollection = database.GetCollection<Movie>("movies");
Console.WriteLine("Before creating a new Index:");
PrintIndexes();

var moviesIndex = new CreateIndexModel<Movie>(Builders<Movie>.IndexKeys
    .Ascending(m => m.Title)
    .Ascending(x => x.Rated));
await moviesCollection.Indexes.CreateOneAsync(moviesIndex);

Console.WriteLine("After creating a new Index:");
PrintIndexes();

void PrintIndexes()
{
    var indexes = moviesCollection.Indexes.List();
    foreach (var index in indexes.ToList())
    {
        Console.WriteLine(index);
    }
}


//LINQ to find all PG-13 movies sorted by title and containing the work "shark" in their plot
var myMovies = await db.Movies
    .Where(m => m.Rated == "PG-13" && m.Plot.Contains("shark"))
    .OrderBy(m => m.Title)
    .ToListAsync();

foreach (var m in myMovies)
{
    Console.WriteLine(m.Title);
}


Movie myMovie1= new Movie {
    Title = "The Rise of EF Core 1",
    Plot = "Entity Framework (EF) Core is a lightweight, extensible, open source and cross-platform version of the popular Entity Framework data access technology.",
    Rated = "G"
};

db.Movies.Add(myMovie1);
await db.SaveChangesAsync();

var dbContext2 = MflixDbContext.Create(database);
dbContext2.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
var myMovie2 = new Movie { Title = "The Rise of EF Core 2" };
dbContext2.Movies.Add(myMovie2);

var myMovie3 = new Movie { Title = "The Rise of EF Core 3" };
dbContext2.Movies.Add(myMovie3);
await dbContext2.SaveChangesAsync();


public class MflixDbContext : DbContext
{
    public DbSet<Movie> Movies { get; init; }

    public static MflixDbContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<MflixDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .LogTo(Console.WriteLine)
            .EnableSensitiveDataLogging()
            .Options);

    public MflixDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Movie>().ToCollection("movies");
    }
}

public class Movie
{
    public ObjectId Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("rated")]
    public string Rated { get; set; }

    [BsonElement("plot")]
    public string Plot { get; set; }

    [BsonElement("adaptedFromBook")]
    public bool? AdaptedFromBook { get; set; }

    [Timestamp]
    public long? Version { get; set; }
}
