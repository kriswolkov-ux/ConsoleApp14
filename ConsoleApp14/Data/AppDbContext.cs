using EfCoreCourse.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCourse.Data;

public class AppDbContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();

    public DbSet<Group> Groups => Set<Group>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
