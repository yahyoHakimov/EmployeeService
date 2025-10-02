using Microsoft.EntityFrameworkCore;
using EmployeeImportSystem.Data;
using EmployeeImportSystem.Data.Repositories;
using EmployeeImportSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURE SERVICES (Dependency Injection)
// ============================================

// Add MVC services (Controllers and Views)
builder.Services.AddControllersWithViews();

// Configure DbContext with SQL Server
// Scoped lifetime - one instance per HTTP request
builder.Services.AddDbContext<EmployeeDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Enable retry on transient failures
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            
            // Set command timeout (seconds)
            sqlOptions.CommandTimeout(30);
        });
    
    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Register Repository (Scoped - tied to HTTP request and DbContext lifetime)
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Register Business Logic Services (Scoped - may use DbContext via repository)
builder.Services.AddScoped<IEmployeeCsvService, EmployeeCsvService>();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// ============================================
// BUILD APPLICATION
// ============================================

var app = builder.Build();

// ============================================
// CONFIGURE HTTP REQUEST PIPELINE (Middleware)
// ============================================

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    // Show detailed error pages
    app.UseDeveloperExceptionPage();
}
else
{
    // Production error handling
    app.UseExceptionHandler("/Home/Error");
    
    // Enable HSTS (HTTP Strict Transport Security)
    app.UseHsts();
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Serve static files (CSS, JS, images) from wwwroot
app.UseStaticFiles();

// Enable routing
app.UseRouting();

// Enable authorization
app.UseAuthorization();

// Configure default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employee}/{action=Index}/{id?}");

// ============================================
// RUN APPLICATION
// ============================================

app.Run();