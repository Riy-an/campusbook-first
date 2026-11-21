namespace CampusBook.Entities;

public class Department
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}

