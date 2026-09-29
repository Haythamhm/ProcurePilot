var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("procurepilot");

var migrations = builder.AddProject<Projects.ProcurePilot_Web>("migrations", launchProfileName: null)
    .WithReference(database)
    .WaitFor(database)
    .WithArgs("--migrate");

builder.AddProject<Projects.ProcurePilot_Web>("web", launchProfileName: "http")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.Build().Run();
