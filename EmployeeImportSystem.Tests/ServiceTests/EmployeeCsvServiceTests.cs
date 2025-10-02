using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeImportSystem.Data.Models;
using EmployeeImportSystem.Data.Repositories;
using EmployeeImportSystem.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace EmployeeImportSystem.Tests.ServiceTests
{
    /// <summary>
    /// Unit tests for CSV import service
    /// Tests parsing, validation, and business logic
    /// </summary>
    public class EmployeeCsvServiceTests
    {
        private readonly Mock<IEmployeeRepository> _repositoryMock;
        private readonly Mock<ILogger<EmployeeCsvService>> _loggerMock;
        private readonly EmployeeCsvService _service;

        public EmployeeCsvServiceTests()
        {
            _repositoryMock = new Mock<IEmployeeRepository>();
            _loggerMock = new Mock<ILogger<EmployeeCsvService>>();
            _service = new EmployeeCsvService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ParseCsvAsync_ShouldParseValidCsv_Successfully()
        {
            var csvContent = @"Personnel_Records.Payroll_Number,Personnel_Records.Forenames,Personnel_Records.Surname,Personnel_Records.Date_of_Birth,Personnel_Records.Telephone,Personnel_Records.Mobile,Personnel_Records.Address,Personnel_Records.Address_2,Personnel_Records.Postcode,Personnel_Records.EMail_Home,Personnel_Records.Start_Date
COOP08,John,William,26/01/1955,12345678,987654231,12 Foreman road,London,GU12 6JW,nomadic20@hotmail.co.uk,18/04/2013";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ParseCsvAsync(stream);

            result.Should().HaveCount(1);
            var employee = result.First();
            employee.PayrollNumber.Should().Be("COOP08");
            employee.Forenames.Should().Be("John");
            employee.Surname.Should().Be("William");
            employee.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ParseCsvAsync_ShouldMarkInvalid_WhenRequiredFieldMissing()
        {
            var csvContent = @"Personnel_Records.Payroll_Number,Personnel_Records.Forenames,Personnel_Records.Surname,Personnel_Records.Date_of_Birth,Personnel_Records.Telephone,Personnel_Records.Mobile,Personnel_Records.Address,Personnel_Records.Address_2,Personnel_Records.Postcode,Personnel_Records.EMail_Home,Personnel_Records.Start_Date
,John,William,26/01/1955,12345678,987654231,12 Foreman road,London,GU12 6JW,test@test.com,18/04/2013";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ParseCsvAsync(stream);

            result.Should().HaveCount(1);
            var employee = result.First();
            employee.IsValid.Should().BeFalse();
            employee.ValidationErrors.Should().Contain(e => e.Contains("Payroll Number is required"));
        }

        [Fact]
        public async Task ParseCsvAsync_ShouldParseUKDates_Correctly()
        {
            var csvContent = @"Personnel_Records.Payroll_Number,Personnel_Records.Forenames,Personnel_Records.Surname,Personnel_Records.Date_of_Birth,Personnel_Records.Telephone,Personnel_Records.Mobile,Personnel_Records.Address,Personnel_Records.Address_2,Personnel_Records.Postcode,Personnel_Records.EMail_Home,Personnel_Records.Start_Date
TEST01,John,Doe,15/03/1990,12345678,987654321,123 Test St,London,AB12 3CD,test@test.com,01/01/2020";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            var result = await _service.ParseCsvAsync(stream);

            var employee = result.First();
            employee.DateOfBirth.Should().NotBeNull();
            employee.DateOfBirth.Value.Day.Should().Be(15);
            employee.DateOfBirth.Value.Month.Should().Be(3);
            employee.DateOfBirth.Value.Year.Should().Be(1990);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ShouldImportValidEmployees()
        {
            var csvContent = @"Personnel_Records.Payroll_Number,Personnel_Records.Forenames,Personnel_Records.Surname,Personnel_Records.Date_of_Birth,Personnel_Records.Telephone,Personnel_Records.Mobile,Personnel_Records.Address,Personnel_Records.Address_2,Personnel_Records.Postcode,Personnel_Records.EMail_Home,Personnel_Records.Start_Date
TEST01,John,Doe,15/03/1990,12345678,987654321,123 Test St,London,AB12 3CD,test@test.com,01/01/2020";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            
            _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Employee>>()))
                .ReturnsAsync(1);

            var result = await _service.ImportFromCsvAsync(stream, "test.csv");

            result.IsSuccess.Should().BeTrue();
            result.SuccessCount.Should().Be(1);
            result.FailureCount.Should().Be(0);
            
            _repositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Employee>>()), Times.Once);
        }

        [Fact]
        public async Task ImportFromCsvAsync_ShouldRejectDuplicatePayrollNumbers()
        {
            var csvContent = @"Personnel_Records.Payroll_Number,Personnel_Records.Forenames,Personnel_Records.Surname,Personnel_Records.Date_of_Birth,Personnel_Records.Telephone,Personnel_Records.Mobile,Personnel_Records.Address,Personnel_Records.Address_2,Personnel_Records.Postcode,Personnel_Records.EMail_Home,Personnel_Records.Start_Date
EXIST01,John,Doe,15/03/1990,12345678,987654321,123 Test St,London,AB12 3CD,test@test.com,01/01/2020";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            _repositoryMock.Setup(r => r.ExistsAsync("EXIST01"))
                .ReturnsAsync(true); // Simulate existing record

            var result = await _service.ImportFromCsvAsync(stream, "test.csv");

            result.SuccessCount.Should().Be(0);
            result.Errors.Should().Contain(e => e.Contains("already exists"));
        }

        [Fact]
        public async Task ValidateCsvAsync_ShouldReturnValidResult_ForGoodData()
        {
            var csvContent = @"Personnel_Records.Payroll_Number,Personnel_Records.Forenames,Personnel_Records.Surname,Personnel_Records.Date_of_Birth,Personnel_Records.Telephone,Personnel_Records.Mobile,Personnel_Records.Address,Personnel_Records.Address_2,Personnel_Records.Postcode,Personnel_Records.EMail_Home,Personnel_Records.Start_Date
TEST01,John,Doe,15/03/1990,12345678,987654321,123 Test St,London,AB12 3CD,test@test.com,01/01/2020";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _service.ValidateCsvAsync(stream);

            result.IsValid.Should().BeTrue();
            result.ValidRowCount.Should().Be(1);
            result.InvalidRowCount.Should().Be(0);
        }
    }
}