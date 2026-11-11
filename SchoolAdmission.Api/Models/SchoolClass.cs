namespace SchoolAdmission.Api.Models;

public class SchoolClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}