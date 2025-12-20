var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GarageUnderground>("garageunderground")
    .WithExternalHttpEndpoints();

builder.Build().Run();
