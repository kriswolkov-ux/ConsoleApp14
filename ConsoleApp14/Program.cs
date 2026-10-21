using EfCoreCourse.Data;
using EfCoreCourse.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// проект сделан на NET 10.00
var builder = Host.CreateApplicationBuilder(args);

// строка подключения лежит в appsettings.json
IConfiguration configuration = builder.Configuration;

string connectionString =
    configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Не найдена строка подключения Default.");

// подключаем EF Core к PostgreSQL, LogTo показывает SQL в консоли
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .LogTo(Console.WriteLine, LogLevel.Information));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var db = scope.ServiceProvider
    .GetRequiredService<AppDbContext>();

try
{
    // начинаем с групп, иначе пустые группы в результат не попадут
    var groups = await db.Groups
        .Include(g => g.Students)
        .OrderBy(g => g.Name)
        .ToListAsync();

    foreach (var group in groups)
    {
        Console.WriteLine($"Группа: {group.Name}");
        Console.WriteLine($"Студентов: {group.Students.Count}");

        if (group.Students.Count == 0)
        {
            Console.WriteLine("В группе пока нет студентов.");
        }
        else
        {
            // студенты уже загружены, поэтому сортируем в памяти
            foreach (var student in group.Students
                .OrderBy(s => s.LastName))
            {
                Console.WriteLine(
                    $"  {student.FirstName} {student.LastName}, " +
                    $"{student.Age}");
            }
        }

        Console.WriteLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
