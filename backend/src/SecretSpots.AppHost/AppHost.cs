var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis", "16-3.4")
    .WithDataVolume();
var db = postgres.AddDatabase("secretspots");

var minio = builder.AddContainer("minio", "minio/minio:latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithVolume("minio-data", "/data")
    .WithEnvironment("MINIO_ROOT_USER", "secretspots")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "secretspots_local_dev");

var api = builder.AddProject<Projects.SecretSpots_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(minio);

var frontend = builder.AddNextJsApp("frontend", "../../../frontend")
    .WithNpm()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
