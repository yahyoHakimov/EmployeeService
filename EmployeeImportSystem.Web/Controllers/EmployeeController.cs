using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EmployeeImportSystem.Data.Repositories;
using EmployeeImportSystem.Web.Services;
using EmployeeImportSystem.Web.Models;

namespace EmployeeImportSystem.Web.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeCsvService _csvService;
        private readonly ILogger<EmployeeController> _logger;

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private const string AllowedExtension = ".csv";

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IEmployeeCsvService csvService,
            ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _csvService = csvService ?? throw new ArgumentNullException(nameof(csvService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Loading employee index page");

                var employees = await _employeeRepository.GetAllAsync();
                
                var viewModels = employees.Select(e => new EmployeeViewModel
                {
                    Id = e.Id,
                    PayrollNumber = e.PayrollNumber,
                    Forenames = e.Forenames,
                    Surname = e.Surname,
                    DateOfBirth = e.DateOfBirth,
                    Telephone = e.Telephone,
                    Mobile = e.Mobile,
                    Address = e.Address,
                    Address2 = e.Address2,
                    Postcode = e.Postcode,
                    EmailHome = e.EmailHome,
                    StartDate = e.StartDate
                }).ToList();

                _logger.LogInformation("Loaded {Count} employees for display", viewModels.Count);

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee index page");
                TempData["ErrorMessage"] = "An error occurred while loading employees.";
                return View(new System.Collections.Generic.List<EmployeeViewModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(Microsoft.AspNetCore.Http.IFormFile csvFile)
        {
            try
            {
                _logger.LogInformation("CSV import requested");

                if (csvFile == null || csvFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    return RedirectToAction(nameof(Index));
                }

                if (csvFile.Length > MaxFileSizeBytes)
                {
                    TempData["ErrorMessage"] = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.";
                    return RedirectToAction(nameof(Index));
                }

                var extension = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
                if (extension != AllowedExtension)
                {
                    TempData["ErrorMessage"] = $"Invalid file type. Only CSV files are allowed.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Processing CSV file: {FileName}, Size: {Size} bytes", 
                    csvFile.FileName, csvFile.Length);

                CsvImportResult result;
                using (var stream = csvFile.OpenReadStream())
                {
                    result = await _csvService.ImportFromCsvAsync(stream, csvFile.FileName);
                }

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {result.SuccessCount} employee(s).";
                    
                    if (result.FailureCount > 0)
                    {
                        TempData["WarningMessage"] = $"{result.FailureCount} row(s) failed validation.";
                    }

                    _logger.LogInformation("CSV import completed successfully. Success: {Success}, Failures: {Failures}", 
                        result.SuccessCount, result.FailureCount);
                }
                else
                {
                    TempData["ErrorMessage"] = "Import failed. Please check the error messages below.";
                    TempData["ImportErrors"] = result.Errors;
                    
                    _logger.LogWarning("CSV import failed. Errors: {ErrorCount}", result.Errors.Count);
                }

                TempData["ImportResult"] = Newtonsoft.Json.JsonConvert.SerializeObject(result);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during CSV import");
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var employees = await _employeeRepository.GetAllAsync();
                
                var viewModels = employees.Select(e => new EmployeeViewModel
                {
                    Id = e.Id,
                    PayrollNumber = e.PayrollNumber,
                    Forenames = e.Forenames,
                    Surname = e.Surname,
                    DateOfBirth = e.DateOfBirth,
                    Telephone = e.Telephone,
                    Mobile = e.Mobile,
                    Address = e.Address,
                    Address2 = e.Address2,
                    Postcode = e.Postcode,
                    EmailHome = e.EmailHome,
                    StartDate = e.StartDate
                }).ToList();

                return Json(new { data = viewModels });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, new { error = "An error occurred while retrieving employees." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var employee = await _employeeRepository.GetByIdAsync(id);
                
                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found." });
                }

                var viewModel = new EmployeeViewModel
                {
                    Id = employee.Id,
                    PayrollNumber = employee.PayrollNumber,
                    Forenames = employee.Forenames,
                    Surname = employee.Surname,
                    DateOfBirth = employee.DateOfBirth,
                    Telephone = employee.Telephone,
                    Mobile = employee.Mobile,
                    Address = employee.Address,
                    Address2 = employee.Address2,
                    Postcode = employee.Postcode,
                    EmailHome = employee.EmailHome,
                    StartDate = employee.StartDate
                };

                return Json(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee details for ID: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving employee details." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] EmployeeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid data.", details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var employee = await _employeeRepository.GetByIdAsync(model.Id);
                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found." });
                }

                employee.PayrollNumber = model.PayrollNumber;
                employee.Forenames = model.Forenames;
                employee.Surname = model.Surname;
                employee.DateOfBirth = model.DateOfBirth;
                employee.Telephone = model.Telephone;
                employee.Mobile = model.Mobile;
                employee.Address = model.Address;
                employee.Address2 = model.Address2;
                employee.Postcode = model.Postcode;
                employee.EmailHome = model.EmailHome;
                employee.StartDate = model.StartDate;

                await _employeeRepository.UpdateAsync(employee);

                _logger.LogInformation("Updated employee with ID: {Id}", model.Id);

                return Json(new { success = true, message = "Employee updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID: {Id}", model?.Id);
                return StatusCode(500, new { error = "An error occurred while updating the employee." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _employeeRepository.DeleteAsync(id);
                
                if (!deleted)
                {
                    return NotFound(new { error = "Employee not found." });
                }

                _logger.LogInformation("Deleted employee with ID: {Id}", id);

                return Json(new { success = true, message = "Employee deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID: {Id}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the employee." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return await GetAll();
                }

                var employees = await _employeeRepository.SearchAsync(term);
                
                var viewModels = employees.Select(e => new EmployeeViewModel
                {
                    Id = e.Id,
                    PayrollNumber = e.PayrollNumber,
                    Forenames = e.Forenames,
                    Surname = e.Surname,
                    DateOfBirth = e.DateOfBirth,
                    Telephone = e.Telephone,
                    Mobile = e.Mobile,
                    Address = e.Address,
                    Address2 = e.Address2,
                    Postcode = e.Postcode,
                    EmailHome = e.EmailHome,
                    StartDate = e.StartDate
                }).ToList();

                return Json(new { data = viewModels });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employees with term: {Term}", term);
                return StatusCode(500, new { error = "An error occurred while searching employees." });
            }
        }
    }
}