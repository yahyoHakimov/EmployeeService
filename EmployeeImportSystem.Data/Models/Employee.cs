using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeImportSystem.Data.Models
{
    /// <summary>
    /// Represents an employee entity mapped to the Employees table
    /// </summary>
    public class Employee
    {
        /// <summary>
        /// Primary key - auto-incremented by database
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Unique payroll identifier for the employee (e.g., COOP08)
        /// </summary>
        [Required(ErrorMessage = "Payroll number is required")]
        [StringLength(50, ErrorMessage = "Payroll number cannot exceed 50 characters")]
        public string PayrollNumber { get; set; } = string.Empty;

        /// <summary>
        /// Employee's first/given name(s)
        /// </summary>
        [Required(ErrorMessage = "Forenames are required")]
        [StringLength(100, ErrorMessage = "Forenames cannot exceed 100 characters")]
        public string Forenames { get; set; } = string.Empty;

        /// <summary>
        /// Employee's surname/last name
        /// </summary>
        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100, ErrorMessage = "Surname cannot exceed 100 characters")]
        public string Surname { get; set; } = string.Empty;

        /// <summary>
        /// Employee's date of birth
        /// </summary>
        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Home/landline telephone number (optional)
        /// </summary>
        [StringLength(20, ErrorMessage = "Telephone cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid telephone format")]
        public string? Telephone { get; set; }

        /// <summary>
        /// Mobile phone number (optional)
        /// </summary>
        [StringLength(20, ErrorMessage = "Mobile cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Invalid mobile format")]
        public string? Mobile { get; set; }

        /// <summary>
        /// Primary address line
        /// </summary>
        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Secondary address line (typically city/town) - optional
        /// </summary>
        [StringLength(100, ErrorMessage = "Address line 2 cannot exceed 100 characters")]
        public string? Address2 { get; set; }

        /// <summary>
        /// Postal/ZIP code
        /// </summary>
        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(20, ErrorMessage = "Postcode cannot exceed 20 characters")]
        public string Postcode { get; set; } = string.Empty;

        /// <summary>
        /// Employee's home email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string EmailHome { get; set; } = string.Empty;

        /// <summary>
        /// Employment start date
        /// </summary>
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Timestamp when this record was created in the system
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Full name property for display purposes (not stored in database)
        /// </summary>
        [NotMapped]
        public string FullName => $"{Forenames} {Surname}";
    }
}