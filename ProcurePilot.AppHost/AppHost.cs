var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("procurepilot");

var migrations = builder.AddProject<Projects.ProcurePilot_Web>("migrations")
    .WithReference(database)
    .WaitFor(database)
    .WithArgs("--migrate");

builder.AddProject<Projects.ProcurePilot_Web>("web")
    .WithReference(database)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
