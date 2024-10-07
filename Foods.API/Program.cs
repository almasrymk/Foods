using Foods.Infrastructure;
using Foods.Infrastructure.Persistence.Data;
using Foods.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddInfrastructureRegistration();
builder.AddServiceRegistration();
builder.Services.AddDbContext<FoodsDbContext>(options => options.UseSqlServer("FoodsDbContext"));
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
