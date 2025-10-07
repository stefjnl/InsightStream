using InsightStream.ServiceDefaults;
using InsightStream.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register infrastructure layer (includes configuration binding)
builder.Services.AddInsightStreamInfrastructure(builder.Configuration);

var app = builder.Build();

// app.MapDefaultEndpoints(); // Commented out for now

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();