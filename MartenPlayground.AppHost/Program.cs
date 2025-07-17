var builder = DistributedApplication.CreateBuilder(args);


var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

var db = postgres.AddDatabase("db", "postgres");

var apiService = builder
    .AddProject<Projects.MartenPlayground_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
