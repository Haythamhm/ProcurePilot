var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("procurepilot");
var invoiceBlobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite.WithDataVolume())
    .AddBlobs("invoiceblobs");

var migrations = builder.AddProject<Projects.ProcurePilot_Web>("migrations", launchProfileName: null)
    .WithReference(database)
    .WithReference(invoiceBlobs)
    .WaitFor(database)
    .WaitFor(invoiceBlobs)
    .WithArgs("--migrate");

var bootstrapSettings = new Dictionary<string, string>
{
    ["Identity:Bootstrap:TenantName"] = "Identity__Bootstrap__TenantName",
    ["Identity:Bootstrap:Email"] = "Identity__Bootstrap__Email",
    ["Identity:Bootstrap:Password"] = "Identity__Bootstrap__Password",
    ["Identity:Bootstrap:DisplayName"] = "Identity__Bootstrap__DisplayName"
};

foreach (var (configurationKey, environmentVariable) in bootstrapSettings)
{
    if (builder.Configuration[configurationKey] is { Length: > 0 } value)
    {
        migrations.WithEnvironment(environmentVariable, value);
    }
}

builder.AddProject<Projects.ProcurePilot_Web>("web", launchProfileName: "http")
    .WithReference(database)
    .WithReference(invoiceBlobs)
    .WaitFor(invoiceBlobs)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.Build().Run();
