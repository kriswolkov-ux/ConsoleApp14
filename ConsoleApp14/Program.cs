using EfCoreCourse.Data;
using EfCoreCourse.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// ветка master это 1 пара, relations 2 пара, crud 3 пара
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
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("=== Студенты учебного центра ===");
        Console.WriteLine("1. Показать всех студентов");
        Console.WriteLine("2. Добавить студента");
        Console.WriteLine("3. Изменить возраст студента");
        Console.WriteLine("4. Удалить студента");
        Console.WriteLine("5. Перевести студента в другую группу");
        Console.WriteLine("0. Выход");
        Console.Write("Выберите пункт: ");

        string? choice = Console.ReadLine();

        if (choice == "0")
        {
            break;
        }

        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await ShowStudents(db);
                break;
            case "2":
                await AddStudent(db);
                break;
            case "3":
                await ChangeAge(db);
                break;
            case "4":
                await DeleteStudent(db);
                break;
            case "5":
                await TransferStudent(db);
                break;
            default:
                Console.WriteLine("Такого пункта нет.");
                break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}

static async Task ShowStudents(AppDbContext db)
{
    var students = await db.Students
        .Include(s => s.Group)
        .OrderBy(s => s.LastName)
        .ToListAsync();

    if (students.Count == 0)
    {
        Console.WriteLine("Студентов пока нет.");
        return;
    }

    foreach (var student in students)
    {
        Console.WriteLine(
            $"{student.Id}: {student.FirstName} {student.LastName}, " +
            $"возраст {student.Age}, группа: {student.Group.Name}");
    }
}

static async Task AddStudent(AppDbContext db)
{
    Console.Write("Имя: ");
    string firstName = Console.ReadLine() ?? "";

    Console.Write("Фамилия: ");
    string lastName = Console.ReadLine() ?? "";

    if (string.IsNullOrWhiteSpace(firstName) ||
        string.IsNullOrWhiteSpace(lastName))
    {
        Console.WriteLine("Имя и фамилия не могут быть пустыми.");
        return;
    }

    Console.Write("Возраст: ");

    if (!int.TryParse(Console.ReadLine(), out int age) || age <= 0)
    {
        Console.WriteLine("Возраст должен быть больше нуля.");
        return;
    }

    Console.Write("Id группы: ");

    if (!int.TryParse(Console.ReadLine(), out int groupId))
    {
        Console.WriteLine("Id группы должен быть числом.");
        return;
    }

    // проверяем, что такая группа существует
    var group = await db.Groups
        .FirstOrDefaultAsync(g => g.Id == groupId);

    if (group is null)
    {
        Console.WriteLine("Группы с таким Id нет.");
        return;
    }

    var student = new Student
    {
        FirstName = firstName,
        LastName = lastName,
        Age = age,
        GroupId = groupId
    };

    db.Students.Add(student);
    await db.SaveChangesAsync();

    Console.WriteLine(
        $"Студент добавлен: {student.FirstName} {student.LastName}, " +
        $"Id = {student.Id}, группа: {group.Name}");
}

static async Task ChangeAge(AppDbContext db)
{
    Console.Write("Id студента: ");

    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("Id должен быть числом.");
        return;
    }

    var student = await db.Students
        .FirstOrDefaultAsync(s => s.Id == id);

    if (student is null)
    {
        Console.WriteLine("Студент с таким Id не найден.");
        return;
    }

    Console.Write("Новый возраст: ");

    if (!int.TryParse(Console.ReadLine(), out int age) || age <= 0)
    {
        Console.WriteLine("Возраст должен быть больше нуля.");
        return;
    }

    // студент уже отслеживается контекстом, поэтому Update() не нужен
    student.Age = age;
    await db.SaveChangesAsync();

    Console.WriteLine(
        $"Возраст изменён: {student.FirstName} {student.LastName} — " +
        $"{student.Age} лет");
}

static async Task DeleteStudent(AppDbContext db)
{
    Console.Write("Id студента: ");

    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("Id должен быть числом.");
        return;
    }

    var student = await db.Students
        .FirstOrDefaultAsync(s => s.Id == id);

    if (student is null)
    {
        Console.WriteLine("Студент с таким Id не найден.");
        return;
    }

    db.Students.Remove(student);
    await db.SaveChangesAsync();

    Console.WriteLine(
        $"Студент удалён: {student.FirstName} {student.LastName}");
}

static async Task TransferStudent(AppDbContext db)
{
    Console.Write("Id студента: ");

    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("Id должен быть числом.");
        return;
    }

    var student = await db.Students
        .Include(s => s.Group)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (student is null)
    {
        Console.WriteLine("Студент с таким Id не найден.");
        return;
    }

    Console.WriteLine(
        $"Студент: {student.FirstName} {student.LastName}");
    Console.WriteLine($"Текущая группа: {student.Group.Name}");

    var groups = await db.Groups
        .OrderBy(g => g.Name)
        .ToListAsync();

    Console.WriteLine();
    Console.WriteLine("Доступные группы:");

    foreach (var group in groups)
    {
        Console.WriteLine($"  {group.Id}: {group.Name}");
    }

    Console.Write("Id новой группы: ");

    if (!int.TryParse(Console.ReadLine(), out int newGroupId))
    {
        Console.WriteLine("Id группы должен быть числом.");
        return;
    }

    var newGroup = groups
        .FirstOrDefault(g => g.Id == newGroupId);

    if (newGroup is null)
    {
        Console.WriteLine("Группы с таким Id нет.");
        return;
    }

    // GroupId обычное свойство, поэтому меняем связь через него
    student.GroupId = newGroupId;
    await db.SaveChangesAsync();

    Console.WriteLine(
        $"Студент переведён в группу: {newGroup.Name}");
}
