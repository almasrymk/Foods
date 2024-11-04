using Foods.Service;
using FluentValidation;
using Foods.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Foods.Infrastructure.Persistence.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddInfrastructureRegistration();
builder.AddServiceRegistration();
builder.Services.AddDbContext<FoodsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("FoodContext")));
builder.Services.AddMediatR(e=> e.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();