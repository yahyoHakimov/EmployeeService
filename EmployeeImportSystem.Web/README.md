---
```markdown
# Employee Import System

CSV import application with ASP.NET Core MVC and SQL Server.

## Quick Start
```bash
# Clone
git clone https://github.com/yahyoHakimov/EmployeeService.git
cd employee-import-system

# Update connection string in EmployeeImportSystem.Web/appsettings.json

# Setup database
dotnet ef database update --project EmployeeImportSystem.Data --startup-project EmployeeImportSystem.Web

# Run
cd EmployeeImportSystem.Web
dotnet run