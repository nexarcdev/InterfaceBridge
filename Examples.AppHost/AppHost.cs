var builder = DistributedApplication.CreateBuilder(args);

var client = builder.AddProject<Projects.Examples_Client>("client");

var server = builder.AddProject<Projects.Examples_Server>("server");

client.WithReference(server).WaitFor(server);

builder.Build().Run();
