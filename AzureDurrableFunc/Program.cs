using durrableShop;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.Configure<SMTPOptions>(
    builder.Configuration.GetSection("Email")
);
builder.Services.AddScoped<IMailingService, MailingService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer((builder.Configuration["HandyCookiesConnection"])));
MappingConfig.Configure();

builder.Build().Run();
