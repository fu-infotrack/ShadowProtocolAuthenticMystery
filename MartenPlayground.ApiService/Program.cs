using Marten;
using Marten.Events.Daemon.Resiliency;
using MartenPlayground.ApiService;
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

builder.Services.AddMassTransit(x =>
{
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
            options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
        }

        options.Policies.ForAllDocuments(x =>
        {
            x.Metadata.CreatedAt.Enabled = true;
        });

        options.Events.UseMandatoryStreamTypeDeclaration = true;
    })
    .AddSubscriptionWithServices<MassTransitPublisherMartenSubscription>(ServiceLifetime.Scoped, o =>
    {
        // This is a default, but just showing what's possible
        o.IncludeArchivedEvents = false;

        // o.FilterIncomingEventsOnStreamType(typeof(Invoice));

        // Process no more than 10 events at a time
        o.Options.BatchSize = 10;

        o.Options.SubscribeFromPresent();
    })
    .UseLightweightSessions()
    .UseNpgsqlDataSource()
    .AddAsyncDaemon(DaemonMode.HotCold);
;



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
        e => e.InitiateRiskExtract(new RiskExtractInitiated(extractId)),
            cancellationToken: cancellationToken);

    // TODO: move this to a consumer
    await repository.GetAndUpdate(
        id,
        e => e.ReceiveRiskExtract(new RiskExtractReceived(extractId)),
            cancellationToken: cancellationToken);

    return Results.Ok();
});

app.MapPost("/entity/{id:Guid}/asic", async (
    [FromServices] IMartenRepository<OrganisationEntity> repository,
    [FromRoute] Guid id,
    CancellationToken cancellationToken) =>
{
    var extractId = Guid.NewGuid();

    await repository.GetAndUpdate(
        id,
        e => e.InitiateAsicExtract(new AsicExtractInitiated(extractId)),
        cancellationToken: cancellationToken);

    // TODO: move this to a consumer
    await repository.GetAndUpdate(
        id,
        e => e.CreateAsicExtractJob(new AsicExtractJobCreated(extractId)),
        cancellationToken: cancellationToken);

    // TODO: move this to a consumer
    await repository.GetAndUpdate(
        id,
        e => e.ReceiveAsicExtract(new AsicExtractReceived(extractId)),
            cancellationToken: cancellationToken);

    // TODO: move this to a consumer
    await repository.GetAndUpdate(
        id,
        e => e.CompleteAsicExtractOrder(new AsicExtractOrderCompleted(extractId)),
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
