using InsightStream.ServiceDefaults;
using InsightStream.Infrastructure.Extensions;
using InsightStream.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register infrastructure layer (includes configuration binding)
builder.Services.AddInsightStreamInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // Add error handling
app.UseCors(); // Add CORS
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();