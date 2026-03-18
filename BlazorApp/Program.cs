using BlazorApp;
using BlazorApp.Data;
using BlazorApp.Layout;
using BlazorApp.Repositories;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddDbContextFactory<TodoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TodoApp")));
builder.Services.AddScoped<TodoQuery>();
builder.Services.AddScoped<TodoCommand>();
WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    IDbContextFactory<TodoDbContext> dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TodoDbContext>>();
    await using TodoDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
    await dbContext.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(PageRoutes.Error, createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute(PageRoutes.NotFound, createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
