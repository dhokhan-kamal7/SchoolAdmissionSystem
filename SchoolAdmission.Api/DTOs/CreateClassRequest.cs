namespace SchoolAdmission.Api.DTOs;

public class CreateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
}