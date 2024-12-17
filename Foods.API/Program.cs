using FluentValidation;
using Foods.Application;
using Foods.Presentation;
using Foods.API.Middleware;
using Foods.Infrastructure;
using Foods.Domain.Abstraction;
using Microsoft.EntityFrameworkCore;
using Foods.Infrastructure.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPresentation().AddInfrastructure().AddApplication();
//builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddDbContext<IFoodsDbContext,FoodsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("FoodContext")));
builder.Services.AddMediatR(e => e.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddMvc();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//}

//app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.HandlerException();

app.Run();