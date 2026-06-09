using Microsoft.EntityFrameworkCore;
using CardTask.Core.Models;

namespace CardTask.Core;

sealed public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<TodoTask> Tasks { get; set; }
}