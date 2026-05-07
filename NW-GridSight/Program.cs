using NW_GridSight.Configuration;
using NW_GridSight.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register HttpClient for EiaService
builder.Services.AddHttpClient<IEiaService, EiaService>();
builder.Services.AddSingleton<IClock, SystemClock>();

// Register DashboardService
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Cache
builder.Services.AddMemoryCache();

// Configure EiaApiOptions
builder.Services.Configure<EiaApiOptions>(
    builder.Configuration.GetSection("EiaApi"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Make the Program class accessible to tests
public partial class Program { }
