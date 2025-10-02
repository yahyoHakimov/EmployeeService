using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using EmployeeImportSystem.Data.Models;
using EmployeeImportSystem.Data.Repositories;
using EmployeeImportSystem.Web.Models;

namespace EmployeeImportSystem.Web.Services
{
    
    public class EmployeeCsvService : IEmployeeCsvService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<EmployeeCsvService> _logger;

        private readonly string[] _dateFormats = new[]
        {
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "dd.MM.yyyy",
            "d.M.yyyy"
        };

        public EmployeeCsvService(
            IEmployeeRepository employeeRepository,
            ILogger<EmployeeCsvService> logger)
        {
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CsvImportResult> ImportFromCsvAsync(Stream fileStream, string fileName)
        {
            var result = new CsvImportResult();

            try
            {
                _logger.LogInformation("Starting CSV import from file: {FileName}", fileName);

                var parsedEmployees = await ParseCsvAsync(fileStream);
                result.TotalRows = parsedEmployees.Count;

                _logger.LogInformation("Parsed {Count} rows from CSV", parsedEmployees.Count);

                var validEmployees = parsedEmployees.Where(e => e.IsValid).ToList();
                var invalidEmployees = parsedEmployees.Where(e => !e.IsValid).ToList();

                foreach (var invalid in invalidEmployees)
                {
                    foreach (var error in invalid.ValidationErrors)
                    {
                        result.Errors.Add($"Row {invalid.RowNumber}: {error}");
                    }
                }

                result.FailureCount = invalidEmployees.Count;

                if (!validEmployees.Any())
                {
                    result.Errors.Add("No valid rows found in CSV file");
                    _logger.LogWarning("No valid employees found in CSV file: {FileName}", fileName);
                    return result;
                }

                var payrollNumbers = validEmployees.Select(e => e.PayrollNumber!).ToList();
                var duplicateChecks = await CheckForDuplicatesAsync(payrollNumbers);
                
                if (duplicateChecks.Any())
                {
                    foreach (var duplicate in duplicateChecks)
                    {
                        result.Errors.Add($"Payroll number '{duplicate}' already exists in database");
                        
                        var duplicateDto = validEmployees.FirstOrDefault(e => e.PayrollNumber == duplicate);
                        if (duplicateDto != null)
                        {
                            duplicateDto.AddError("Payroll number already exists");
                            validEmployees.Remove(duplicateDto);
                            result.FailureCount++;
                        }
                    }
                }

                var employeeEntities = validEmployees.Select(MapToEntity).ToList();

                if (employeeEntities.Any())
                {
                    try
                    {
                        var savedCount = await _employeeRepository.AddRangeAsync(employeeEntities);
                        result.SuccessCount = savedCount;

                        result.ImportedEmployees = employeeEntities
                            .Select(MapToViewModel)
                            .OrderBy(e => e.Surname)
                            .ThenBy(e => e.Forenames)
                            .ToList();

                        _logger.LogInformation("Successfully imported {Count} employees from {FileName}", 
                            savedCount, fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving employees to database");
                        result.Errors.Add($"Database error: {ex.Message}");
                        result.FailureCount = validEmployees.Count;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during CSV import from {FileName}", fileName);
                result.Errors.Add($"Import failed: {ex.Message}");
                return result;
            }
        }

        public async Task<CsvValidationResult> ValidateCsvAsync(Stream fileStream)
        {
            var result = new CsvValidationResult();

            try
            {
                _logger.LogInformation("Validating CSV file");

                var parsedEmployees = await ParseCsvAsync(fileStream);

                var validEmployees = parsedEmployees.Where(e => e.IsValid).ToList();
                var invalidEmployees = parsedEmployees.Where(e => !e.IsValid).ToList();

                result.ValidRowCount = validEmployees.Count;
                result.InvalidRowCount = invalidEmployees.Count;
                foreach (var invalid in invalidEmployees)
                {
                    foreach (var error in invalid.ValidationErrors)
                    {
                        result.ValidationErrors.Add($"Row {invalid.RowNumber}: {error}");
                    }
                }

                if (validEmployees.Any())
                {
                    var payrollNumbers = validEmployees.Select(e => e.PayrollNumber!).ToList();
                    var duplicates = await CheckForDuplicatesAsync(payrollNumbers);
                    
                    foreach (var duplicate in duplicates)
                    {
                        result.ValidationErrors.Add($"Payroll number '{duplicate}' already exists in database");
                        result.ValidRowCount--;
                        result.InvalidRowCount++;
                    }
                }

                result.PreviewData = validEmployees.Take(10).ToList();

                result.IsValid = result.ValidRowCount > 0 && !result.ValidationErrors.Any();

                _logger.LogInformation("CSV validation complete. Valid: {Valid}, Invalid: {Invalid}", 
                    result.ValidRowCount, result.InvalidRowCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating CSV file");
                result.ValidationErrors.Add($"Validation failed: {ex.Message}");
                result.IsValid = false;
                return result;
            }
        }

        public async Task<List<EmployeeImportDto>> ParseCsvAsync(Stream fileStream)
        {
            var employees = new List<EmployeeImportDto>();
            
            try
            {
                if (fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim, // Trim whitespace
                    MissingFieldFound = null, // Don't throw on missing fields
                    HeaderValidated = null, // Don't validate headers
                    BadDataFound = null, // Handle bad data gracefully
                    IgnoreBlankLines = true,
                    DetectDelimiter = true, // Auto-detect delimiter
                };

                using var reader = new StreamReader(fileStream);
                using var csv = new CsvReader(reader, config);

                await csv.ReadAsync();
                csv.ReadHeader();

                int rowNumber = 1; // Start from 1 (after header)

                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    
                    try
                    {
                        var dto = new EmployeeImportDto
                        {
                            RowNumber = rowNumber
                        };

                        dto.PayrollNumber = GetFieldValue(csv, "Personnel_Records.Payroll_Number");
                        dto.Forenames = GetFieldValue(csv, "Personnel_Records.Forenames");
                        dto.Surname = GetFieldValue(csv, "Personnel_Records.Surname");
                        dto.Telephone = GetFieldValue(csv, "Personnel_Records.Telephone");
                        dto.Mobile = GetFieldValue(csv, "Personnel_Records.Mobile");
                        dto.Address = GetFieldValue(csv, "Personnel_Records.Address");
                        dto.Address2 = GetFieldValue(csv, "Personnel_Records.Address_2");
                        dto.Postcode = GetFieldValue(csv, "Personnel_Records.Postcode");
                        dto.EmailHome = GetFieldValue(csv, "Personnel_Records.EMail_Home");

                        dto.DateOfBirthRaw = GetFieldValue(csv, "Personnel_Records.Date_of_Birth");
                        dto.DateOfBirth = ParseDate(dto.DateOfBirthRaw, "Date of Birth", dto);

                        dto.StartDateRaw = GetFieldValue(csv, "Personnel_Records.Start_Date");
                        dto.StartDate = ParseDate(dto.StartDateRaw, "Start Date", dto);

                        ValidateEmployeeDto(dto);

                        employees.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing row {RowNumber}", rowNumber);
                        
                        var errorDto = new EmployeeImportDto
                        {
                            RowNumber = rowNumber
                        };
                        errorDto.AddError($"Failed to parse row: {ex.Message}");
                        employees.Add(errorDto);
                    }
                }

                _logger.LogInformation("Successfully parsed {Count} rows from CSV", employees.Count);
                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file");
                throw new InvalidOperationException("Failed to parse CSV file. Please ensure it's a valid CSV format.", ex);
            }
        }

        private string? GetFieldValue(CsvReader csv, string fieldName)
        {
            try
            {
                return csv.GetField<string>(fieldName)?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private DateTime? ParseDate(string? dateString, string fieldName, EmployeeImportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                dto.AddError($"{fieldName} is required");
                return null;
            }

            foreach (var format in _dateFormats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }

            if (DateTime.TryParse(dateString, new CultureInfo("en-GB"), DateTimeStyles.None, out DateTime parsed))
            {
                return parsed;
            }

            dto.AddError($"{fieldName} '{dateString}' is not a valid date. Expected format: dd/MM/yyyy");
            return null;
        }

        private void ValidateEmployeeDto(EmployeeImportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PayrollNumber))
            {
                dto.AddError("Payroll Number is required");
            }
            else if (dto.PayrollNumber.Length > 50)
            {
                dto.AddError("Payroll Number cannot exceed 50 characters");
            }

            if (string.IsNullOrWhiteSpace(dto.Forenames))
            {
                dto.AddError("Forenames are required");
            }
            else if (dto.Forenames.Length > 100)
            {
                dto.AddError("Forenames cannot exceed 100 characters");
            }

            if (string.IsNullOrWhiteSpace(dto.Surname))
            {
                dto.AddError("Surname is required");
            }
            else if (dto.Surname.Length > 100)
            {
                dto.AddError("Surname cannot exceed 100 characters");
            }

            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                dto.AddError("Address is required");
            }
            else if (dto.Address.Length > 200)
            {
                dto.AddError("Address cannot exceed 200 characters");
            }

            if (string.IsNullOrWhiteSpace(dto.Postcode))
            {
                dto.AddError("Postcode is required");
            }
            else if (dto.Postcode.Length > 20)
            {
                dto.AddError("Postcode cannot exceed 20 characters");
            }

            if (string.IsNullOrWhiteSpace(dto.EmailHome))
            {
                dto.AddError("Email is required");
            }
            else if (!IsValidEmail(dto.EmailHome))
            {
                dto.AddError("Email format is invalid");
            }
            else if (dto.EmailHome.Length > 100)
            {
                dto.AddError("Email cannot exceed 100 characters");
            }

            if (!string.IsNullOrWhiteSpace(dto.Telephone) && dto.Telephone.Length > 20)
            {
                dto.AddError("Telephone cannot exceed 20 characters");
            }

            if (!string.IsNullOrWhiteSpace(dto.Mobile) && dto.Mobile.Length > 20)
            {
                dto.AddError("Mobile cannot exceed 20 characters");
            }

            if (!string.IsNullOrWhiteSpace(dto.Address2) && dto.Address2.Length > 100)
            {
                dto.AddError("Address line 2 cannot exceed 100 characters");
            }

            if (!dto.DateOfBirth.HasValue)
            {
                dto.AddError("Date of Birth is required");
            }
            else
            {
                if (dto.DateOfBirth.Value > DateTime.Today)
                {
                    dto.AddError("Date of Birth cannot be in the future");
                }

                var age = DateTime.Today.Year - dto.DateOfBirth.Value.Year;
                if (dto.DateOfBirth.Value > DateTime.Today.AddYears(-age))
                {
                    age--;
                }

                if (age < 16)
                {
                    dto.AddError("Employee must be at least 16 years old");
                }

                if (age > 100)
                {
                    dto.AddError("Date of Birth appears invalid (age > 100 years)");
                }
            }

            if (!dto.StartDate.HasValue)
            {
                dto.AddError("Start Date is required");
            }
            else
            {
                if (dto.StartDate.Value > DateTime.Today)
                {
                    dto.AddError("Start Date cannot be in the future");
                }

                if (dto.StartDate.Value < DateTime.Today.AddYears(-50))
                {
                    dto.AddError("Start Date appears invalid (more than 50 years ago)");
                }

                if (dto.DateOfBirth.HasValue && dto.StartDate.Value < dto.DateOfBirth.Value)
                {
                    dto.AddError("Start Date cannot be before Date of Birth");
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<string>> CheckForDuplicatesAsync(List<string> payrollNumbers)
        {
            var duplicates = new List<string>();

            foreach (var payrollNumber in payrollNumbers)
            {
                var exists = await _employeeRepository.ExistsAsync(payrollNumber);
                if (exists)
                {
                    duplicates.Add(payrollNumber);
                }
            }

            return duplicates;
        }

        private Employee MapToEntity(EmployeeImportDto dto)
        {
            return new Employee
            {
                PayrollNumber = dto.PayrollNumber!,
                Forenames = dto.Forenames!,
                Surname = dto.Surname!,
                DateOfBirth = dto.DateOfBirth!.Value,
                Telephone = dto.Telephone,
                Mobile = dto.Mobile,
                Address = dto.Address!,
                Address2 = dto.Address2,
                Postcode = dto.Postcode!,
                EmailHome = dto.EmailHome!,
                StartDate = dto.StartDate!.Value,
                CreatedDate = DateTime.UtcNow
            };
        }

        private EmployeeViewModel MapToViewModel(Employee entity)
        {
            return new EmployeeViewModel
            {
                Id = entity.Id,
                PayrollNumber = entity.PayrollNumber,
                Forenames = entity.Forenames,
                Surname = entity.Surname,
                DateOfBirth = entity.DateOfBirth,
                Telephone = entity.Telephone,
                Mobile = entity.Mobile,
                Address = entity.Address,
                Address2 = entity.Address2,
                Postcode = entity.Postcode,
                EmailHome = entity.EmailHome,
                StartDate = entity.StartDate
            };
        }
    }
}