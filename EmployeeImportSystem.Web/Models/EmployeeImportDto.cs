using System;
using System.Collections.Generic;

namespace EmployeeImportSystem.Web.Models
{
    public class EmployeeImportDto
    {
        public int RowNumber { get; set; }
        public string? PayrollNumber { get; set; }
        public string? Forenames { get; set; }
        public string? Surname { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? DateOfBirthRaw { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? Postcode { get; set; }
        public string? EmailHome { get; set; }
        public DateTime? StartDate { get; set; }
        public string? StartDateRaw { get; set; }
        public bool IsValid { get; set; } = true;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public void AddError(string error)
        {
            IsValid = false;
            ValidationErrors.Add(error);
        }
    }
}