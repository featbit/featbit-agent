using Api.Setup;
using Api.Persistence;

// register services
var webApplication = WebApplication.CreateBuilder(args)
    .RegisterServices(args)
    .Build();

// ensure DbContext table created
{
    using var scope = webApplication.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FbDbContext>();
    context.EnsureTableCreated();
}

// setup middleware and run application
webApplication
    .SetupMiddleware()
    .Run();