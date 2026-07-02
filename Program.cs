using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserManagementApp.Repository;
using UserManagementApp.Repositories;
using UserManagementApp.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------- SERVICES --------------------

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dapper Global Config
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Dapper Repository Layer
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IAppealRepository, AppealRepository>();
builder.Services.AddScoped<IAuditLogService, AuditLogRepository>();

// Service Layer
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IAppealService, AppealService>();

// JWT Configuration
var jwtKeyString = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
var key = Encoding.UTF8.GetBytes(jwtKeyString);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// -------------------- DATABASE MIGRATION --------------------
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    try
    {
        using (var connection = new Npgsql.NpgsqlConnection(connectionString))
        {
            connection.Open();
            // Add ActionByAdmin to usermaster
            using (var cmd = new Npgsql.NpgsqlCommand("ALTER TABLE usermaster ADD COLUMN IF NOT EXISTS actionbyadmin VARCHAR(100);", connection))
            {
                cmd.ExecuteNonQuery();
            }
            // Add ActionByAdmin to userdocuments
            using (var cmd = new Npgsql.NpgsqlCommand("ALTER TABLE userdocuments ADD COLUMN IF NOT EXISTS actionbyadmin VARCHAR(100);", connection))
            {
                cmd.ExecuteNonQuery();
            }
            
            // Populate ActionByAdmin for existing records
            using (var cmd = new Npgsql.NpgsqlCommand("UPDATE usermaster SET actionbyadmin = 'Rahul1' WHERE status != 'Pending' AND actionbyadmin IS NULL;", connection))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new Npgsql.NpgsqlCommand("UPDATE userdocuments SET actionbyadmin = 'Rahul1' WHERE status != 'Pending' AND actionbyadmin IS NULL;", connection))
            {
                cmd.ExecuteNonQuery();
            }

            // --- New Migrations: KYC Upload Limits & Super Admin ---
            using (var cmd = new Npgsql.NpgsqlCommand("ALTER TABLE usermaster ADD COLUMN IF NOT EXISTS upload_attempts INTEGER NOT NULL DEFAULT 0;", connection))
            {
                cmd.ExecuteNonQuery();
            }

            // Ensure Super Admin role exists in rolemaster
            using (var cmd = new Npgsql.NpgsqlCommand("INSERT INTO rolemaster (roleid, rolename) SELECT 3, 'Super Admin' WHERE NOT EXISTS (SELECT 1 FROM rolemaster WHERE roleid = 3);", connection))
            {
                cmd.ExecuteNonQuery();
            }

            // Promote Rahul1 to Super Admin
            using (var cmd = new Npgsql.NpgsqlCommand("UPDATE usermaster SET roleid = 3 WHERE username = 'Rahul1' AND roleid != 3;", connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration failed: " + ex.Message);
    }
}

// -------------------- SNAPSHOT GENERATION (OLD DATA) --------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var auditService = scope.ServiceProvider.GetRequiredService<UserManagementApp.Services.IAuditLogService>();
        var userService = scope.ServiceProvider.GetRequiredService<UserManagementApp.Services.IUserService>();
        var docService = scope.ServiceProvider.GetRequiredService<UserManagementApp.Services.IDocumentService>();

        var allLogs = auditService.GetAllLogs();
        if (!allLogs.Any())
        {
            var allUsers = userService.GetAllUsers();
            foreach (var user in allUsers)
            {
                var detailsJson = $"{{\"email\": \"{user.Email}\", \"roleId\": \"{user.RoleId}\"}}";
                auditService.LogAction("USER_INITIALIZED", "System", "User", user.UserId, detailsJson, "Info");

                if (user.Status == "Approved")
                {
                    auditService.LogAction("KYC_INITIALIZED", "System", "User", user.UserId, "{\"note\": \"KYC already approved (old data)\"}", "Info");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Snapshot generation failed: " + ex.Message);
    }
}

// -------------------- MIDDLEWARE --------------------

app.UseMiddleware<UserManagementApp.Middlewares.GlobalExceptionMiddleware>();
app.UseStatusCodePagesWithReExecute("/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// -------------------- ROUTING --------------------

// Area routing (MUST be before default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();