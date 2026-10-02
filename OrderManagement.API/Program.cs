using OrderManagement.API.Middleware;
using OrderManagement.Repository.Extensions;
using OrderManagement.Service.Extensions;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Order Management API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/orderapi-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 30));

    builder.Services.AddControllers();
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
    
    builder.Services.AddOpenApi();
    
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    builder.Services.AddRepositories(connectionString);
    
    builder.Services.AddServices();
    
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    
    var app = builder.Build();
    
    await app.Services.MigrateAsync();
    
    app.UseMiddleware<ExceptionHandlingMiddleware>();    
    app.UseMiddleware<CorrelationIdMiddleware>();        
    app.UseMiddleware<IdempotencyMiddleware>();          

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.UseDeveloperExceptionPage();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? "unknown");
            if (httpContext.Items.TryGetValue("CorrelationId", out var cid) && cid != null)
                diagnosticContext.Set("CorrelationId", cid);
        };
    });

    app.UseCors();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
