using ConsoleApp14;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCourse.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // таблица в базе называется "Students" — EF берёт имя из DbSet
        public DbSet<Student> Students { get; set; } = null!;
    }
}
