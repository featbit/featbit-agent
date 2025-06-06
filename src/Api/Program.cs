using Api.Setup;

var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("FeatBit Agent needs an relay proxy API key to run and the ApiKey is not configured.");
    return;
}

builder.RegisterServices()
    .Build()
    .SetupMiddleware()
    .Run();