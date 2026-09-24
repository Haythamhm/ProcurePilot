var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ProcurePilot_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
