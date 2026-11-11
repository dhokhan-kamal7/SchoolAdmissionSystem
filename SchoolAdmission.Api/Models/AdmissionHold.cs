namespace SchoolAdmission.Api.Models;

public class AdmissionHold
{
    public Guid Id { get; set; }

    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;

    public string Status { get; set; } = "HELD";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
}