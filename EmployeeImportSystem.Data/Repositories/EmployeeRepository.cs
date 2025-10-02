using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EmployeeImportSystem.Data.Models;

namespace EmployeeImportSystem.Data.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(
            EmployeeDbContext context,
            ILogger<EmployeeRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

 
        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all employees from database");

                var employees = await _context.Employees
                    .AsNoTracking()
                    .OrderBy(e => e.Surname)
                    .ThenBy(e => e.Forenames)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} employees", employees.Count);
                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees");
                throw;
            }
        }

       
        public async Task<Employee?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving employee with ID: {Id}", id);

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {Id} not found", id);
                }

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID: {Id}", id);
                throw;
            }
        }

        public async Task<Employee?> GetByPayrollNumberAsync(string payrollNumber)
        {
            try
            {
                _logger.LogInformation("Retrieving employee with payroll number: {PayrollNumber}", payrollNumber);

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.PayrollNumber == payrollNumber);

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with payroll number: {PayrollNumber}", payrollNumber);
                throw;
            }
        }

        
        public async Task<bool> ExistsAsync(string payrollNumber)
        {
            try
            {
                return await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(e => e.PayrollNumber == payrollNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of payroll number: {PayrollNumber}", payrollNumber);
                throw;
            }
        }

        
        public async Task<Employee> AddAsync(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    throw new ArgumentNullException(nameof(employee));
                }

                _logger.LogInformation("Adding employee: {PayrollNumber}", employee.PayrollNumber);

                var exists = await ExistsAsync(employee.PayrollNumber);
                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Employee with payroll number {employee.PayrollNumber} already exists");
                }

                await _context.Employees.AddAsync(employee);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully added employee with ID: {Id}", employee.Id);
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee: {PayrollNumber}", employee?.PayrollNumber);
                throw;
            }
        }

        
        public async Task<int> AddRangeAsync(IEnumerable<Employee> employees)
        {
            try
            {
                if (employees == null || !employees.Any())
                {
                    _logger.LogWarning("AddRangeAsync called with null or empty collection");
                    return 0;
                }

                var employeeList = employees.ToList();
                _logger.LogInformation("Adding {Count} employees in bulk", employeeList.Count);

                var payrollNumbers = employeeList.Select(e => e.PayrollNumber).ToList();
                var duplicatesInBatch = payrollNumbers
                    .GroupBy(p => p)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicatesInBatch.Any())
                {
                    throw new InvalidOperationException(
                        $"Duplicate payroll numbers in batch: {string.Join(", ", duplicatesInBatch)}");
                }

                var existingPayrollNumbers = await _context.Employees
                    .Where(e => payrollNumbers.Contains(e.PayrollNumber))
                    .Select(e => e.PayrollNumber)
                    .ToListAsync();

                if (existingPayrollNumbers.Any())
                {
                    throw new InvalidOperationException(
                        $"Following payroll numbers already exist: {string.Join(", ", existingPayrollNumbers)}");
                }

                await _context.Employees.AddRangeAsync(employeeList);

                
                var savedCount = await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully added {Count} employees", savedCount);
                return savedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employees in bulk");
                throw;
            }
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    throw new ArgumentNullException(nameof(employee));
                }

                _logger.LogInformation("Updating employee with ID: {Id}", employee.Id);

                var existing = await _context.Employees.FindAsync(employee.Id);
                if (existing == null)
                {
                    throw new InvalidOperationException($"Employee with ID {employee.Id} not found");
                }

                _context.Entry(existing).CurrentValues.SetValues(employee);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated employee with ID: {Id}", employee.Id);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID: {Id}", employee?.Id);
                throw;
            }
        }

      
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting employee with ID: {Id}", id);

                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {Id} not found for deletion", id);
                    return false;
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted employee with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID: {Id}", id);
                throw;
            }
        }

        
        public async Task<int> GetCountAsync()
        {
            try
            {
                return await _context.Employees.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee count");
                throw;
            }
        }

       
        public async Task<IEnumerable<Employee>> SearchAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllAsync();
                }

                _logger.LogInformation("Searching employees with term: {SearchTerm}", searchTerm);

                var searchPattern = $"%{searchTerm}%";

                var employees = await _context.Employees
                    .AsNoTracking()
                    .Where(e => EF.Functions.Like(e.Forenames, searchPattern) ||
                               EF.Functions.Like(e.Surname, searchPattern) ||
                               EF.Functions.Like(e.PayrollNumber, searchPattern))
                    .OrderBy(e => e.Surname)
                    .ThenBy(e => e.Forenames)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} employees matching '{SearchTerm}'", 
                    employees.Count, searchTerm);

                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employees with term: {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}