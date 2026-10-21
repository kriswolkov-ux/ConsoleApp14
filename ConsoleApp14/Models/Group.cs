using System.Collections.Generic;

namespace EfCoreCourse.Models;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Student> Students { get; set; }
        = new List<Student>();
}