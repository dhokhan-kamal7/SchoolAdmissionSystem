namespace SchoolAdmission.Api.DTOs;

public class CreateHoldRequest
{
    public string StudentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
}