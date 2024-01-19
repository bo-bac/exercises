using Microsoft.EntityFrameworkCore;
namespace Core;

internal class Db : DbContext
{
    public DbSet<Hash> Hashs { get; set; }

    public Db(DbContextOptions<Db> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hash>()
            .HasKey(x => x.Id);
    }
}
