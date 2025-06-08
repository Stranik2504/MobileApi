using Database;
using MobileApi.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ManagerObject<Config>>(_ =>
{
    var manager = new ManagerObject<Config>("files/config.json");
    manager.Load();

    return manager;
});

builder.Services.AddSingleton<IDatabase>(x =>
{
    var config = x.GetRequiredService<ManagerObject<Config>>();

    if (config.Obj == null)
        throw new Exception("Config is null");

    var db = new Database.MySql(
        config.Obj.MainDbHost,
        config.Obj.MainDbPort,
        config.Obj.MainDbName,
        config.Obj.MainDbUser,
        config.Obj.MainDbPassword,
        x.GetRequiredService<ILogger<Database.MySql>>()
    );
    db.Start();

    return db;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var db = app.Services.GetRequiredService<IDatabase>();

MigrationManager migrationManager = new(db, 1);
await migrationManager.Migrate();

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

app.UseSwagger(options =>
{
    options.RouteTemplate = "mobile/{documentName}/swagger.json";
});
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api";
    options.SwaggerEndpoint("/mobile/v1/swagger.json", "MobileAPI v1");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();