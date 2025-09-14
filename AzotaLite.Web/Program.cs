using AzotaLite.Web.Data;
using AzotaLite.Web.Models;
using AzotaLite.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Đăng ký DbContext dùng ConnectionString "DefaultConnection"
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 2) MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // <-- cần có để MapRazorPages hoạt động

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTeacher", policy => policy.RequireRole("Teacher"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

var app = builder.Build();

// middleware mặc định...
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// BẮT BUỘC để bật các trang /Identity/Account/*
app.MapRazorPages();
await IdentitySeeder.SeedAsync(app.Services);
app.Run();
