using System.Reflection;
using System.Security.Cryptography;
using JasperFx.Core.IoC;
using Marten;
using MartenPlayground.ApiService;
using MartenPlayground.ApiService.Application;
using MartenPlayground.ApiService.Domain;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddNpgsqlDataSource("db");
builder.Services.AddScoped<IMartenRepository<OrganisationEntity>, MartenRepository<OrganisationEntity>>();

builder.Services.Scan(scan =>
{
    scan.TheCallingAssembly();
    scan.ConnectImplementationsToTypesClosing(typeof(IStreamIntegrationEventHandler<>), ServiceLifetime.Scoped);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(Assembly.GetExecutingAssembly());

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services
    .AddMarten(options =>
    {
        options.UseSystemTextJsonForSerialization();

        // TODO: build migration into release pipeline
        if (builder.Environment.IsDevelopment())
        {
            options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
        }

        options.Policies.ForAllDocuments(x =>
        {
            x.Metadata.CreatedAt.Enabled = true;
        });

        // Disable the absurdly verbose Npgsql logging
        options.DisableNpgsqlLogging = true;

        options.Events.UseMandatoryStreamTypeDeclaration = true;

        options.Policies.ForAllDocuments(x =>
        {
            x.Metadata.CausationId.Enabled = true;
            x.Metadata.CorrelationId.Enabled = true;
            x.Metadata.Headers.Enabled = true;

            // This column is "opt in"
            x.Metadata.CreatedAt.Enabled = true;
        });
    })
    .AddSubscriptionWithServices<MassTransitPublisherMartenSubscription>(ServiceLifetime.Scoped, o =>
    {
        // This is a default, but just showing what's possible
        o.IncludeArchivedEvents = false;

        // Process no more than 10 events at a time
        o.Options.BatchSize = 10;

        o.Options.SubscribeFromPresent();
    })
    .UseLightweightSessions()
    .UseNpgsqlDataSource()
    .AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.HotCold);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("swagger", _ => _.Servers = []);
}

app.MapPost("/entity", async (
    [FromServices] IMartenRepository<OrganisationEntity> repository, CancellationToken cancellationToken) =>
{
    EntityCreated entityCreated = new(Guid.NewGuid());

    var entity = OrganisationEntity.Initialise(entityCreated);

    await repository.Add(entityCreated.EntityId, entity, cancellationToken);

    return entity;
});

app.MapPost("/entity/{id:Guid}/risk", async (
    [FromServices] IMartenRepository<OrganisationEntity> repository,
    [FromRoute] Guid id,
    CancellationToken cancellationToken) =>
{
    var extractId = Guid.NewGuid();

    await repository.GetAndUpdate(
        id,
        e => e.InitiateRiskExtract(new RiskExtractInitiated(extractId, id)),
            cancellationToken: cancellationToken);

    // TODO: move this to a consumer
    await repository.GetAndUpdate(
        id,
        e => e.ReceiveRiskExtract(new RiskExtractReceived(extractId, id)),
            cancellationToken: cancellationToken);

    return Results.Ok();
});

app.MapPost("/entity/{id:Guid}/asic", async (
    [FromServices] IMartenRepository<OrganisationEntity> repository,
    [FromRoute] Guid id,
    CancellationToken cancellationToken) =>
{
    await repository.GetAndUpdate(
        id,
        e =>
        {
            for (int i = 0; i < 5; i++)
            {
                e.InitiateAsicExtract(new AsicExtractInitiated(id, Guid.NewGuid(), $"{RandomNumberGenerator.GetInt32(100_000_000, 1_000_000_000)}"));
            }
        },
        cancellationToken: cancellationToken);

    return Results.Ok();
});

app.MapGet("/entity/{id:Guid}", async (
    [FromServices] IMartenRepository<OrganisationEntity> repository,
    [FromRoute] Guid id,
    CancellationToken cancellationToken) =>
{
    var entity = await repository.Find(id, cancellationToken);

    return entity;
});

app.MapDefaultEndpoints();

app.Run();
