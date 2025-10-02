using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeImportSystem.Data.Models;

namespace EmployeeImportSystem.Data.Repositories
{
    /// <summary>
    /// Repository interface for Employee entity operations
    /// Provides abstraction over data access layer
    /// </summary>
    public interface IEmployeeRepository
    {
        /// <summary>
        /// Retrieves all employees from the database
        /// Orders by surname ascending by default
        /// </summary>
        /// <returns>Collection of all employees</returns>
        Task<IEnumerable<Employee>> GetAllAsync();

        /// <summary>
        /// Retrieves a single employee by their unique identifier
        /// </summary>
        /// <param name="id">Employee ID</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves an employee by their payroll number
        /// Useful for checking duplicates before import
        /// </summary>
        /// <param name="payrollNumber">Unique payroll identifier</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetByPayrollNumberAsync(string payrollNumber);

        /// <summary>
        /// Checks if an employee with given payroll number exists
        /// Efficient for validation without loading full entity
        /// </summary>
        /// <param name="payrollNumber">Payroll number to check</param>
        /// <returns>True if exists, false otherwise</returns>
        Task<bool> ExistsAsync(string payrollNumber);

        /// <summary>
        /// Adds a single employee to the database
        /// </summary>
        /// <param name="employee">Employee entity to add</param>
        /// <returns>The added employee with generated ID</returns>
        Task<Employee> AddAsync(Employee employee);

        /// <summary>
        /// Adds multiple employees in a single transaction
        /// Used for bulk CSV imports
        /// More efficient than multiple individual inserts
        /// </summary>
        /// <param name="employees">Collection of employees to add</param>
        /// <returns>Number of employees successfully added</returns>
        Task<int> AddRangeAsync(IEnumerable<Employee> employees);

        /// <summary>
        /// Updates an existing employee's information
        /// </summary>
        /// <param name="employee">Employee with updated data</param>
        /// <returns>Updated employee</returns>
        Task<Employee> UpdateAsync(Employee employee);

        /// <summary>
        /// Deletes an employee by ID
        /// </summary>
        /// <param name="id">ID of employee to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Gets count of all employees
        /// Useful for statistics and validation
        /// </summary>
        /// <returns>Total number of employees</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// Searches employees by surname or forenames
        /// Case-insensitive search
        /// </summary>
        /// <param name="searchTerm">Term to search for</param>
        /// <returns>Matching employees</returns>
        Task<IEnumerable<Employee>> SearchAsync(string searchTerm);
    }
}