using System.Reflection;
using Api.Endpoints;
using Api.Extensions;
using FluentValidation;
using Infrastructure.DataBase;
using Infrastructure.PaymentSystem;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    
    opt.SwaggerDoc("v1", new() { Title = "Daily Rent API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, 
                    Id = JwtBearerDefaults.AuthenticationScheme
                },
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuth(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddAutoMapper(typeof(Program), typeof(PaymentService));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddBackgroundJobs(builder.Configuration);
builder.Services.AddHttpClients(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddRepositories();
builder.Services.AddUtils();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()   
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
builder.Host.UseSerilog();

var app = builder.Build();

app.UseAntiforgery();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "public"))
});
app.UseAuthentication();
app.UseAuthorization();

app.MapUsersEndpoints();
app.MapAuthEndpoints();
app.MapPropertiesEndpoints();
app.MapBookingsEndpoints();
app.MapCompensationRequestsEndpoints();
app.MapReviewEndpoints();
app.MapGet("/", context => context.Response.SendFileAsync("public/index.html") );
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
}
app.Run();