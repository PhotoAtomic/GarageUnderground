var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GarageUnderground>("garageunderground");

builder.Build().Run();
