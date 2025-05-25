using BookApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation(); // Razor Pages için

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddJsonFile("appsettings.json");
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddSignalR();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapHub<ScrapingHub>("/scrapinghub");



// Statik dosyalar (CSS, JS, vs.) ve yönlendirme
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapRazorPages();
app.Run();
