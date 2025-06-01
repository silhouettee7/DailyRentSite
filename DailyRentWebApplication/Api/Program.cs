using Api.Endpoints;
using Api.Extensions;
using Infrastructure.DataBase;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddApiServices();
builder.Services.AddRepositories();
builder.Services.AddUtils();

var app = builder.Build();

app.MapUsersEndpoints();
app.MapAuthEndpoints();

app.Run();