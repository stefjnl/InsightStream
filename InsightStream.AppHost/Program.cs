var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.InsightStream_Api>("api")
    .WithExternalHttpEndpoints();

builder.Build().Run();