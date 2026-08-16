var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("helpdesk");

var api = builder.AddProject<Projects.HelpDesk_API>("api")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.HelpDesk_Web>("web")
    .WithReference(api)
    .WithReference(postgres)
    .WaitFor(api);

builder.Build().Run();