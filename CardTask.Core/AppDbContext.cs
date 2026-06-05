using Microsoft.EntityFrameworkCore;
using CardTask.Core.Models;

namespace CardTask.Core;

sealed public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    // Note: You will add Tasks and Courses collections here in Sprint 2!
}