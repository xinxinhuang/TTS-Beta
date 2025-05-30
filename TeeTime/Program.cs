using Microsoft.EntityFrameworkCore;
using TeeTime.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using TeeTime.Services;
using TeeTime.Services.Logging;
using Microsoft.IO;

var builder = WebApplication.CreateBuilder(args);

// Add User Secrets
builder.Configuration.AddUserSecrets<Program>();

// Debug connection string
Console.WriteLine($"CONNECTION STRING: {builder.Configuration.GetConnectionString("TeeTimeDatabase")}");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Add logging service
builder.Services.AddLoggingService(builder.Configuration);
builder.Services.AddSingleton<RecyclableMemoryStreamManager>();

// Add database context
builder.Services.AddDbContext<TeeTimeDbContext>(options => {
    var connStr = builder.Configuration.GetConnectionString("TeeTimeDatabase");
    Console.WriteLine($"EF USING: {connStr}");
    options.UseSqlServer(connStr,
        sqlOptions => sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "Beta"));
    
    // Enable sensitive data logging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Register services
builder.Services.AddScoped<ITeeTimeService, TeeTimeService>();
builder.Services.AddScoped<TeeTime.Data.Interfaces.IStandingTeeTimeRepository, TeeTime.Data.Repositories.StandingTeeTimeRepository>();
builder.Services.AddScoped<TeeTime.Data.Interfaces.ITeeSheetRepository, TeeTime.Data.Repositories.TeeSheetRepository>();
builder.Services.AddScoped<TeeTime.Services.TeeSheetService>();

// Add cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Initialize the database with seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var logger = services.GetRequiredService<ILoggerService>();
        logger.Info("Initializing database with seed data");
        SeedData.Initialize(services);
        logger.Info("Database initialization completed");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerService>();
        logger.Error(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Only enable request/response logging in development
    app.UseRequestResponseLogging();
}

app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next();
});
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();