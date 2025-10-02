using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeImportSystem.Web.Models
{
    
    public class EmployeeViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Payroll Number")]
        public string PayrollNumber { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string Forenames { get; set; } = string.Empty;

        [Display(Name = "Surname")]
        public string Surname { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName => $"{Forenames} {Surname}";

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Age")]
        public int Age => DateTime.Now.Year - DateOfBirth.Year;

        [Display(Name = "Telephone")]
        public string? Telephone { get; set; }

        [Display(Name = "Mobile")]
        public string? Mobile { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "City/Town")]
        public string? Address2 { get; set; }

        [Display(Name = "Postcode")]
        public string Postcode { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string EmailHome { get; set; } = string.Empty;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Years of Service")]
        public int YearsOfService => DateTime.Now.Year - StartDate.Year;
    }
}