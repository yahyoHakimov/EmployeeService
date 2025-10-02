using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmployeeImportSystem.Web.Models;

namespace EmployeeImportSystem.Web.Services
{
    
    public interface IEmployeeCsvService
    {
        Task<CsvImportResult> ImportFromCsvAsync(Stream fileStream, string fileName);
        Task<CsvValidationResult> ValidateCsvAsync(Stream fileStream);
        Task<List<EmployeeImportDto>> ParseCsvAsync(Stream fileStream);
    }
}