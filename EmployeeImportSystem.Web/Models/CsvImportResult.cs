using System.Collections.Generic;
using System.Linq;

namespace EmployeeImportSystem.Web.Models
{
    
    public class CsvImportResult
    {
        
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalRows { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<EmployeeViewModel> ImportedEmployees { get; set; } = new List<EmployeeViewModel>();
        public bool IsSuccess => SuccessCount > 0 && Errors.Count == 0;
        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
    }
}