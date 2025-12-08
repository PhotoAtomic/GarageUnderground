var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GarageUnderground>("garageunderground")
    .WithEnvironment("LiteDb__DatabasePath", "/app/data/garage-underground.db");

builder.Build().Run();
