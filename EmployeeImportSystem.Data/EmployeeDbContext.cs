using Microsoft.EntityFrameworkCore;
using EmployeeImportSystem.Data.Models;

namespace EmployeeImportSystem.Data
{
   
    public class EmployeeDbContext : DbContext
    {
        
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) 
            : base(options)
        {
        }

       
        public DbSet<Employee> Employees { get; set; } = null!;

        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employees");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.PayrollNumber)
                      .IsUnique()
                      .HasDatabaseName("IX_Employees_PayrollNumber");

                entity.HasIndex(e => e.Surname)
                      .HasDatabaseName("IX_Employees_Surname");

                entity.HasIndex(e => new { e.Forenames, e.Surname })
                      .HasDatabaseName("IX_Employees_FullName");

                entity.Property(e => e.PayrollNumber)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(e => e.Forenames)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.Surname)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.EmailHome)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.Address)
                      .HasMaxLength(200)
                      .IsRequired();

                entity.Property(e => e.Address2)
                      .HasMaxLength(100)
                      .IsRequired(false); // Nullable

                entity.Property(e => e.Postcode)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.Property(e => e.Telephone)
                      .HasMaxLength(20)
                      .IsRequired(false); // Nullable

                entity.Property(e => e.Mobile)
                      .HasMaxLength(20)
                      .IsRequired(false); // Nullable

                entity.Property(e => e.DateOfBirth)
                      .HasColumnType("date")
                      .IsRequired();

                entity.Property(e => e.StartDate)
                      .HasColumnType("date")
                      .IsRequired();

                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();
            });
        }
    }
}