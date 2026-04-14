var builder = DistributedApplication.CreateBuilder(args);

// Fixed credentials so the persistent volume stays valid across restarts.
// Matches the guest/guest default from docker-compose.yml.
var rabbitUser = builder.AddParameter("rabbitmq-username", "guest");
var rabbitPass = builder.AddParameter("rabbitmq-password", "guest", secret: true);

var rabbitmq = builder.AddRabbitMQ("RabbitMQ", rabbitUser, rabbitPass)
    .WithManagementPlugin()          // management UI on port 15672
    .WithDataVolume("rabbitmq_data"); // volume persists data between runs; container stops with AppHost

builder.AddProject<Projects.ModularMonolithEventDriven_Api>("api")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.Build().Run();
