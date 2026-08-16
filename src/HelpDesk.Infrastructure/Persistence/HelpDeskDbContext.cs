using HelpDesk.Domain.Categories;
using HelpDesk.Domain.Comments;
using HelpDesk.Domain.Platforms;
using HelpDesk.Domain.Tickets;
using HelpDesk.Domain.Users;

using Microsoft.EntityFrameworkCore;


namespace HelpDesk.Infrastructure.Persistence;

public class HelpDeskDbContext : DbContext
{
    public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Platform> Platforms => Set<Platform>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey("Id");
        modelBuilder.Entity<User>().HasIndex("Email").IsUnique();
        modelBuilder.Entity<User>().Property("Email").IsRequired();
        modelBuilder.Entity<User>().Property("FirstName").IsRequired();
        modelBuilder.Entity<User>().Property("LastName").IsRequired();
        modelBuilder.Entity<User>().Property("PasswordHash").IsRequired();

        modelBuilder.Entity<Ticket>().HasKey("Id");
        modelBuilder.Entity<Ticket>().Property("Title").IsRequired();
        modelBuilder.Entity<Ticket>().Property("Status").IsRequired();
        modelBuilder.Entity<Ticket>().Property("Priority").IsRequired();

        modelBuilder.Entity<Comment>().HasKey("Id");
        modelBuilder.Entity<Comment>().Property("Message").IsRequired();

        modelBuilder.Entity<Category>().HasKey("Id");
        modelBuilder.Entity<Category>().Property("Name").IsRequired();
        modelBuilder.Entity<Category>().HasIndex("Name").IsUnique();

        modelBuilder.Entity<Platform>().HasKey("Id");
        modelBuilder.Entity<Platform>().Property("Name").IsRequired();
        modelBuilder.Entity<Platform>().HasIndex("Name").IsUnique();
    }
}