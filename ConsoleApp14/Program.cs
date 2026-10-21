using EfCoreCourse.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// проект сделан на NET 10.00
var builder = Host.CreateApplicationBuilder(args);

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

// спрашиваем диапазон возраста и номер страницы
Console.Write("Минимальный возраст: ");
int minAge = int.Parse(Console.ReadLine()!);

Console.Write("Максимальный возраст: ");
int maxAge = int.Parse(Console.ReadLine()!);

Console.Write("Номер страницы: ");

if (!int.TryParse(Console.ReadLine(), out int page) || page < 1)
{
    page = 1;
}

const int pageSize = 3;

try
{
    var students = await db.Students
        .Where(s => s.Age >= minAge && s.Age <= maxAge)
        .OrderBy(s => s.LastName)
        .ThenBy(s => s.FirstName)
        .ThenBy(s => s.Id)
        .Select(s => new
        {
            s.FirstName,
            s.LastName,
            s.Age
        })
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    Console.WriteLine();
    Console.WriteLine("=== Результат поиска ===");

    if (students.Count == 0)
    {
        Console.WriteLine("Студентов в указанном диапазоне нет.");
    }
    else
    {
        foreach (var student in students)
        {
            Console.WriteLine(
                $"{student.FirstName} {student.LastName}, {student.Age}");
        }
    }

    // есть ли хоть один студент младше 18
    bool hasMinors = await db.Students
        .AnyAsync(s => s.Age < 18);

    Console.WriteLine();
    Console.WriteLine($"Есть несовершеннолетние студенты: {hasMinors}");

    Console.WriteLine();
    Console.WriteLine("=== Аналитика учебного центра ===");
    Console.WriteLine();

    int count = await db.Students.CountAsync();

    Console.WriteLine($"Всего студентов: {count}");

    if (count == 0)
    {
        Console.WriteLine("Данных для отчёта нет.");
        return;
    }

    double averageAge = await db.Students
        .AverageAsync(s => s.Age);

    int minAgeInDb = await db.Students
        .MinAsync(s => s.Age);

    int maxAgeInDb = await db.Students
        .MaxAsync(s => s.Age);

    Console.WriteLine($"Средний возраст: {averageAge:F1}");
    Console.WriteLine($"Минимальный возраст: {minAgeInDb}");
    Console.WriteLine($"Максимальный возраст: {maxAgeInDb}");

    Console.WriteLine();
    Console.WriteLine("Распределение по возрасту:");

    // сколько студентов каждого возраста
    var statistics = await db.Students
        .GroupBy(s => s.Age)
        .Select(g => new
        {
            Age = g.Key,
            Count = g.Count()
        })
        .OrderBy(x => x.Age)
        .ToListAsync();

    foreach (var item in statistics)
    {
        Console.WriteLine(
            $"{item.Age} {AgeWord(item.Age)} — {item.Count}");
    }

    // все ли достигли 18 лет
    bool allAdults = await db.Students
        .AllAsync(s => s.Age >= 18);

    Console.WriteLine();
    Console.WriteLine($"Все студенты совершеннолетние: {allAdults}");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}

// окончание для возраста: 1 год, 2 года, 5 лет
static string AgeWord(int age)
{
    if (age % 100 >= 11 && age % 100 <= 14)
    {
        return "лет";
    }

    if (age % 10 == 1)
    {
        return "год";
    }

    if (age % 10 >= 2 && age % 10 <= 4)
    {
        return "года";
    }

    return "лет";
}
