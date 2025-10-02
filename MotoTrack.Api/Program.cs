using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoTrack.Api.Data;
using MotoTrack.Api.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<MotoTrackContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=mototrack.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    var xml = System.IO.Path.Combine(AppContext.BaseDirectory, "MotoTrack.Api.xml");
    opt.IncludeXmlComments(xml, includeControllerXmlComments: true);
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    });

builder.Services.AddRouting(o => o.LowercaseUrls = true);

var app = builder.Build();

// Apply migrations/seed (for demo purposes only)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MotoTrackContext>();
    db.Database.EnsureCreated();
    Seed.Load(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();