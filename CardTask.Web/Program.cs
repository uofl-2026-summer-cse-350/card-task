using CardTask.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Database Connection Context Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CONFIGURE SECURE MIDDLEWARE COOKIES
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
    });

// 1. ADD SESSION MEMORY CONTAINER SERVICES (CRITICAL FOR CUSTOM LABELS)
// ADD SESSION MEMORY CONTAINER SERVICES
builder.Services.AddDistributedMemoryCache(); // Sets up the backend RAM storage pool
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session duration timeout rule

    // FIX: Assign these properties directly through the Cookie sub-object wrapper!
    options.Cookie.HttpOnly = true;                 // Security cookie guard protection
    options.Cookie.IsEssential = true;              // Enforces cookie delivery even if tracking cookies are blocked
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 2. ACTIVATE THE SESSION MIDDLEWARE PIPELINE INTERCEPTOR
// CRITICAL ORDER: UseSession MUST run after UseRouting and BEFORE MapRazorPages
app.UseSession();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();