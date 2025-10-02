using System.Collections.Generic;
using System.Linq;

namespace EmployeeImportSystem.Web.Models
{

    public class CsvValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public int ValidRowCount { get; set; }
        public int InvalidRowCount { get; set; }
        public List<EmployeeImportDto> PreviewData { get; set; } = new List<EmployeeImportDto>();
        public string Message => IsValid 
            ? $"CSV is valid. {ValidRowCount} rows ready for import." 
            : $"CSV has {ValidationErrors.Count} validation errors.";
    }
}