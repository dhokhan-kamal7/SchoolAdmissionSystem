using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();
}