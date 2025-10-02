using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeImportSystem.Data;
using EmployeeImportSystem.Data.Models;
using EmployeeImportSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace EmployeeImportSystem.Tests.RepositoryTests
{
    /// <summary>
    /// Unit tests for EmployeeRepository
    /// Tests CRUD operations using in-memory database
    /// </summary>
    public class EmployeeRepositoryTests : IDisposable
    {
        private readonly EmployeeDbContext _context;
        private readonly EmployeeRepository _repository;
        private readonly Mock<ILogger<EmployeeRepository>> _loggerMock;

        public EmployeeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<EmployeeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EmployeeDbContext(options);
            _loggerMock = new Mock<ILogger<EmployeeRepository>>();
            _repository = new EmployeeRepository(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmployeesSortedBySurname()
        {
            var employees = new List<Employee>
            {
                CreateTestEmployee("JACK13", "Jerry", "Jackson"),
                CreateTestEmployee("COOP08", "John", "William"),
                CreateTestEmployee("TEST01", "Alice", "Anderson")
            };
            await _context.Employees.AddRangeAsync(employees);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Should().HaveCount(3);
            result.First().Surname.Should().Be("Anderson");
            result.Last().Surname.Should().Be("William");
        }

        [Fact]
        public async Task AddAsync_ShouldAddEmployeeSuccessfully()
        {
            var employee = CreateTestEmployee("TEST01", "Test", "User");

            var result = await _repository.AddAsync(employee);

            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            
            var saved = await _context.Employees.FindAsync(result.Id);
            saved.Should().NotBeNull();
            saved.PayrollNumber.Should().Be("TEST01");
        }

        [Fact]
        public async Task AddAsync_ShouldThrowException_WhenDuplicatePayrollNumber()
        {
            var employee1 = CreateTestEmployee("DUPLICATE", "First", "Employee");
            await _repository.AddAsync(employee1);

            var employee2 = CreateTestEmployee("DUPLICATE", "Second", "Employee");

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _repository.AddAsync(employee2)
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEmployee_WhenExists()
        {
            var employee = CreateTestEmployee("TEST01", "Test", "User");
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(employee.Id);

            result.Should().NotBeNull();
            result.PayrollNumber.Should().Be("TEST01");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenPayrollNumberExists()
        {
            var employee = CreateTestEmployee("EXISTS", "Test", "User");
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            var result = await _repository.ExistsAsync("EXISTS");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenPayrollNumberNotExists()
        {
            var result = await _repository.ExistsAsync("NOTEXISTS");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddRangeAsync_ShouldAddMultipleEmployees()
        {
            var employees = new List<Employee>
            {
                CreateTestEmployee("BULK01", "First", "Employee"),
                CreateTestEmployee("BULK02", "Second", "Employee"),
                CreateTestEmployee("BULK03", "Third", "Employee")
            };

            var count = await _repository.AddRangeAsync(employees);

            count.Should().Be(3);
            var allEmployees = await _context.Employees.ToListAsync();
            allEmployees.Should().HaveCount(3);
        }

        [Fact]
        public async Task AddRangeAsync_ShouldThrowException_WhenDuplicatesInBatch()
        {
            var employees = new List<Employee>
            {
                CreateTestEmployee("DUP", "First", "Employee"),
                CreateTestEmployee("DUP", "Second", "Employee")
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _repository.AddRangeAsync(employees)
            );
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEmployeeSuccessfully()
        {
            var employee = CreateTestEmployee("UPDATE", "Original", "Name");
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            employee.Forenames = "Updated";
            employee.Surname = "NewName";
            await _repository.UpdateAsync(employee);

            var updated = await _context.Employees.FindAsync(employee.Id);
            updated.Forenames.Should().Be("Updated");
            updated.Surname.Should().Be("NewName");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteEmployee_WhenExists()
        {
            var employee = CreateTestEmployee("DELETE", "Test", "User");
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            var result = await _repository.DeleteAsync(employee.Id);

            result.Should().BeTrue();
            var deleted = await _context.Employees.FindAsync(employee.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
        {
            var result = await _repository.DeleteAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task SearchAsync_ShouldFindEmployeesByName()
        {
            await _context.Employees.AddRangeAsync(new[]
            {
                CreateTestEmployee("SEARCH01", "John", "Smith"),
                CreateTestEmployee("SEARCH02", "Jane", "Johnson"),
                CreateTestEmployee("SEARCH03", "Bob", "Williams")
            });
            await _context.SaveChangesAsync();

            var result = await _repository.SearchAsync("John");

            // Assert
            result.Should().HaveCount(2); // John Smith and Jane Johnson
        }

        // Helper method to create test employees
        private Employee CreateTestEmployee(string payrollNumber, string forenames, string surname)
        {
            return new Employee
            {
                PayrollNumber = payrollNumber,
                Forenames = forenames,
                Surname = surname,
                DateOfBirth = new DateTime(1980, 1, 1),
                Telephone = "12345678",
                Mobile = "87654321",
                Address = "123 Test Street",
                Address2 = "Test City",
                Postcode = "TEST123",
                EmailHome = $"{payrollNumber.ToLower()}@test.com",
                StartDate = new DateTime(2020, 1, 1)
            };
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}