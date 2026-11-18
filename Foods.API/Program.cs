using Serilog;
using System.Text;
using FluentValidation;
using Foods.Application;
using Foods.Presentation;
using Foods.Infrastructure;
using Foods.API.Middleware;
using Microsoft.OpenApi.Models;
using Foods.Domain.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Foods.Application.Abstraction.Jwt;
using Foods.Infrastructure.Persistence.Jwt;
using Foods.Infrastructure.Persistence.Data;
using Foods.Infrastructure.Persistence.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPresentation().AddInfrastructure().AddApplication();
builder.Services.AddDbContext<IFoodsDbContext, FoodsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("FoodContext"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Foods.API.Middleware.RequestPipeLine<,>));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Foods API", Version = "v1" });

    c.CustomSchemaIds(type => type.FullName);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new string[] {}
        }
    });
});
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(e =>
    {
        e.RequireHttpsMetadata = false;
        e.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
string[] cliModes = ["--seed", "--menu", "--stats", "--tidy", "--align", "--images", "--notify"];
if (!args.Any(cliModes.Contains))
    builder.Services.AddHostedService<Foods.API.Notifications.NotificationWorker>();
var logPath = builder.Configuration["Log:Path"];
if (string.IsNullOrWhiteSpace(logPath))
    logPath = "logs/FoodsLogs.txt";
if (!Path.IsPathRooted(logPath))
    logPath = Path.Combine(builder.Environment.ContentRootPath, logPath);
var logger = new LoggerConfiguration();
if (args.Contains("--seed") || args.Contains("--menu") || args.Contains("--stats") || args.Contains("--tidy") || args.Contains("--align"))
    logger = logger.MinimumLevel.Warning();
logger = logger.WriteTo.Console();
try
{
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    logger = logger.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
}
catch
{
}
Log.Logger = logger.CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200").AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddMvc(options =>
{
    options.Conventions.Add(new Foods.API.Security.MasterDataAdminConvention());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = (FoodsDbContext)scope.ServiceProvider.GetRequiredService<IFoodsDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        IF COL_LENGTH('Meals', 'IconPath') IS NULL
            ALTER TABLE Meals ADD IconPath nvarchar(max) NULL;
        IF COL_LENGTH('Meals', 'Color') IS NULL
            ALTER TABLE Meals ADD Color nvarchar(max) NULL;
        IF COL_LENGTH('Meals', 'TextColor') IS NULL
            ALTER TABLE Meals ADD TextColor nvarchar(max) NULL;
        IF COL_LENGTH('Meals', 'Sort') IS NULL
            ALTER TABLE Meals ADD Sort int NOT NULL CONSTRAINT DF_Meals_Sort DEFAULT 0;
        IF OBJECT_ID(N'Cuisines', N'U') IS NULL
        BEGIN
            CREATE TABLE Cuisines (
                Id uniqueidentifier NOT NULL PRIMARY KEY,
                Name nvarchar(max) NULL,
                IconPath nvarchar(max) NULL,
                Color nvarchar(max) NULL,
                TextColor nvarchar(max) NULL,
                Sort int NOT NULL CONSTRAINT DF_Cuisines_Sort DEFAULT 0,
                IsDeleted bit NOT NULL CONSTRAINT DF_Cuisines_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_Cuisines_Status DEFAULT 0
            );
        END
        IF COL_LENGTH('Items', 'CuisineId') IS NULL
            ALTER TABLE Items ADD CuisineId uniqueidentifier NULL;
        IF COL_LENGTH('Requests', 'CreatedAt') IS NULL
            ALTER TABLE Requests ADD CreatedAt datetime2 NULL;
        IF COL_LENGTH('Requests', 'PrintedAt') IS NULL
            ALTER TABLE Requests ADD PrintedAt datetime2 NULL;
        IF COL_LENGTH('Requests', 'ReorderedAt') IS NULL
            ALTER TABLE Requests ADD ReorderedAt datetime2 NULL;
        IF COL_LENGTH('Requests', 'SharedAt') IS NULL
            ALTER TABLE Requests ADD SharedAt datetime2 NULL;
        IF COL_LENGTH('Requests', 'PrintCount') IS NULL
            ALTER TABLE Requests ADD PrintCount int NOT NULL CONSTRAINT DF_Requests_PrintCount DEFAULT 0;
        IF COL_LENGTH('Requests', 'ShareCount') IS NULL
            ALTER TABLE Requests ADD ShareCount int NOT NULL CONSTRAINT DF_Requests_ShareCount DEFAULT 0;
        IF COL_LENGTH('Contents', 'PurchaseMode') IS NULL
            ALTER TABLE Contents ADD PurchaseMode int NOT NULL CONSTRAINT DF_Contents_PurchaseMode DEFAULT 0;
        IF COL_LENGTH('Contents', 'PackageName') IS NULL
            ALTER TABLE Contents ADD PackageName nvarchar(100) NULL;
        IF COL_LENGTH('Contents', 'PurchaseStep') IS NULL
            ALTER TABLE Contents ADD PurchaseStep decimal(18,3) NOT NULL CONSTRAINT DF_Contents_PurchaseStep DEFAULT 0;
        IF COL_LENGTH('Contents', 'PurchaseIncrement') IS NULL
            ALTER TABLE Contents ADD PurchaseIncrement decimal(18,3) NOT NULL CONSTRAINT DF_Contents_PurchaseIncrement DEFAULT 0;
        IF COL_LENGTH('Contents', 'PurchaseUnitPrice') IS NULL
            ALTER TABLE Contents ADD PurchaseUnitPrice decimal(18,2) NOT NULL CONSTRAINT DF_Contents_PurchaseUnitPrice DEFAULT 0;
        IF COL_LENGTH('Contents', 'PurchaseMinAmount') IS NULL
            ALTER TABLE Contents ADD PurchaseMinAmount decimal(18,2) NOT NULL CONSTRAINT DF_Contents_PurchaseMinAmount DEFAULT 0;
        IF OBJECT_ID(N'ItemImages', N'U') IS NULL
        BEGIN
            CREATE TABLE ItemImages (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ItemImages PRIMARY KEY,
                ItemId uniqueidentifier NOT NULL,
                ImagePath nvarchar(max) NULL,
                Sort int NOT NULL CONSTRAINT DF_ItemImages_Sort DEFAULT 0,
                IsDeleted bit NOT NULL CONSTRAINT DF_ItemImages_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_ItemImages_Status DEFAULT 0
            );
            CREATE INDEX IX_ItemImages_ItemId ON ItemImages(ItemId);
        END
        IF OBJECT_ID(N'UserItemContents', N'U') IS NULL
        BEGIN
            CREATE TABLE UserItemContents (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_UserItemContents PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                ItemId uniqueidentifier NOT NULL,
                ContentId uniqueidentifier NOT NULL,
                UnitId uniqueidentifier NOT NULL,
                Quantity decimal(18,3) NOT NULL CONSTRAINT DF_UserItemContents_Quantity DEFAULT 0,
                IsRemoved bit NOT NULL CONSTRAINT DF_UserItemContents_IsRemoved DEFAULT 0,
                IsDeleted bit NOT NULL CONSTRAINT DF_UserItemContents_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_UserItemContents_Status DEFAULT 0
            );
            CREATE INDEX IX_UserItemContents_User_Item ON UserItemContents(UserId, ItemId);
        END
        IF OBJECT_ID(N'UserItemSteps', N'U') IS NULL
        BEGIN
            CREATE TABLE UserItemSteps (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_UserItemSteps PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                ItemId uniqueidentifier NOT NULL,
                Sort int NOT NULL CONSTRAINT DF_UserItemSteps_Sort DEFAULT 0,
                Body nvarchar(max) NULL,
                IsDeleted bit NOT NULL CONSTRAINT DF_UserItemSteps_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_UserItemSteps_Status DEFAULT 0
            );
            CREATE INDEX IX_UserItemSteps_User_Item ON UserItemSteps(UserId, ItemId);
        END
        IF OBJECT_ID(N'ContentImages', N'U') IS NULL
        BEGIN
            CREATE TABLE ContentImages (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ContentImages PRIMARY KEY,
                ContentId uniqueidentifier NOT NULL,
                ImagePath nvarchar(max) NULL,
                Sort int NOT NULL CONSTRAINT DF_ContentImages_Sort DEFAULT 0,
                IsDeleted bit NOT NULL CONSTRAINT DF_ContentImages_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_ContentImages_Status DEFAULT 0
            );
            CREATE INDEX IX_ContentImages_ContentId ON ContentImages(ContentId);
        END
        IF COL_LENGTH('Items', 'ShowToCustomer') IS NULL
            ALTER TABLE Items ADD ShowToCustomer bit NOT NULL CONSTRAINT DF_Items_ShowToCustomer DEFAULT 1;
        IF COL_LENGTH('ItemContents', 'IsOptional') IS NULL
            ALTER TABLE ItemContents ADD IsOptional bit NOT NULL CONSTRAINT DF_ItemContents_IsOptional DEFAULT 0;
        IF COL_LENGTH('ItemContents', 'IsDefault') IS NULL
            ALTER TABLE ItemContents ADD IsDefault bit NOT NULL CONSTRAINT DF_ItemContents_IsDefault DEFAULT 1;
        IF OBJECT_ID(N'ItemParts', N'U') IS NULL
        BEGIN
            CREATE TABLE ItemParts (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ItemParts PRIMARY KEY,
                ItemId uniqueidentifier NOT NULL,
                PartItemId uniqueidentifier NOT NULL,
                IsOptional bit NOT NULL CONSTRAINT DF_ItemParts_IsOptional DEFAULT 0,
                IsDefault bit NOT NULL CONSTRAINT DF_ItemParts_IsDefault DEFAULT 1,
                Sort int NOT NULL CONSTRAINT DF_ItemParts_Sort DEFAULT 0,
                IsDeleted bit NOT NULL CONSTRAINT DF_ItemParts_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_ItemParts_Status DEFAULT 0
            );
            CREATE INDEX IX_ItemParts_ItemId ON ItemParts(ItemId);
        END
        IF OBJECT_ID(N'UserItemChoices', N'U') IS NULL
        BEGIN
            CREATE TABLE UserItemChoices (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_UserItemChoices PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                ItemId uniqueidentifier NOT NULL,
                Kind int NOT NULL CONSTRAINT DF_UserItemChoices_Kind DEFAULT 0,
                RefId uniqueidentifier NOT NULL,
                Selected bit NOT NULL CONSTRAINT DF_UserItemChoices_Selected DEFAULT 0,
                IsDeleted bit NOT NULL CONSTRAINT DF_UserItemChoices_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_UserItemChoices_Status DEFAULT 0
            );
            CREATE INDEX IX_UserItemChoices_User_Item ON UserItemChoices(UserId, ItemId);
        END
        IF COL_LENGTH('Contents', 'Price') IS NULL
            ALTER TABLE Contents ADD Price decimal(18,3) NOT NULL CONSTRAINT DF_Contents_Price DEFAULT 0;
        IF COL_LENGTH('Contents', 'PriceQuantity') IS NULL
            ALTER TABLE Contents ADD PriceQuantity decimal(18,3) NOT NULL CONSTRAINT DF_Contents_PriceQuantity DEFAULT 1;
        IF COL_LENGTH('Contents', 'PriceUnitId') IS NULL
            ALTER TABLE Contents ADD PriceUnitId uniqueidentifier NULL;
        IF OBJECT_ID(N'UserContentPrices', N'U') IS NULL
        BEGIN
            CREATE TABLE UserContentPrices (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_UserContentPrices PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                ContentId uniqueidentifier NOT NULL,
                Price decimal(18,3) NOT NULL CONSTRAINT DF_UserContentPrices_Price DEFAULT 0,
                PriceQuantity decimal(18,3) NOT NULL CONSTRAINT DF_UserContentPrices_PriceQuantity DEFAULT 1,
                PriceUnitId uniqueidentifier NULL,
                IsDeleted bit NOT NULL CONSTRAINT DF_UserContentPrices_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_UserContentPrices_Status DEFAULT 0
            );
            CREATE INDEX IX_UserContentPrices_User ON UserContentPrices(UserId, ContentId);
        END
        IF COL_LENGTH('Cuisines', 'CountryId') IS NULL
            ALTER TABLE Cuisines ADD CountryId uniqueidentifier NULL;
        IF COL_LENGTH('Cuisines', 'ImagePath') IS NULL
            ALTER TABLE Cuisines ADD ImagePath nvarchar(max) NULL;
        IF COL_LENGTH('FamilyMembers', 'Relation') IS NULL
            ALTER TABLE FamilyMembers ADD Relation nvarchar(40) NULL;
        IF COL_LENGTH('FamilyMembers', 'ImagePath') IS NULL
            ALTER TABLE FamilyMembers ADD ImagePath nvarchar(400) NULL;
        IF OBJECT_ID(N'Notifications', N'U') IS NOT NULL AND COL_LENGTH('Notifications', 'Image') IS NULL
            ALTER TABLE Notifications ADD Image nvarchar(400) NULL;
        IF COL_LENGTH('Users', 'ImagePath') IS NULL
            ALTER TABLE Users ADD ImagePath nvarchar(max) NULL;
        IF COL_LENGTH('Users', 'LastLoginAt') IS NULL
            ALTER TABLE Users ADD LastLoginAt datetime2 NULL;
        IF COL_LENGTH('FamilyMembers', 'BirthDate') IS NULL
            ALTER TABLE FamilyMembers ADD BirthDate datetime2 NULL;
        IF OBJECT_ID(N'UserSettings', N'U') IS NULL
        BEGIN
            CREATE TABLE UserSettings (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_UserSettings PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                Language nvarchar(10) NOT NULL CONSTRAINT DF_UserSettings_Language DEFAULT N'ar',
                NotifyLogin bit NOT NULL CONSTRAINT DF_UserSettings_NotifyLogin DEFAULT 1,
                NotifyWelcome bit NOT NULL CONSTRAINT DF_UserSettings_NotifyWelcome DEFAULT 1,
                NotifyBirthday bit NOT NULL CONSTRAINT DF_UserSettings_NotifyBirthday DEFAULT 1,
                NotifyOrders bit NOT NULL CONSTRAINT DF_UserSettings_NotifyOrders DEFAULT 1,
                NotifyPlanning bit NOT NULL CONSTRAINT DF_UserSettings_NotifyPlanning DEFAULT 1,
                IsDeleted bit NOT NULL CONSTRAINT DF_UserSettings_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_UserSettings_Status DEFAULT 0
            );
            CREATE UNIQUE INDEX IX_UserSettings_UserId ON UserSettings(UserId);
        END
        IF OBJECT_ID(N'UserSettings', N'U') IS NOT NULL AND COL_LENGTH('UserSettings', 'UsePrices') IS NULL
            ALTER TABLE UserSettings ADD UsePrices bit NOT NULL CONSTRAINT DF_UserSettings_UsePrices DEFAULT 0;
        IF OBJECT_ID(N'UserSettings', N'U') IS NOT NULL AND COL_LENGTH('UserSettings', 'PriceCountryId') IS NULL
            ALTER TABLE UserSettings ADD PriceCountryId uniqueidentifier NULL;
        -- Per-country versions (2026-11-17): currency per country, ingredient cuisine + per-country ingredient rows,
        -- per-country recipe rows (CountryId NULL = the general recipe) and per-country customer edits.
        IF COL_LENGTH('Countries', 'Currency') IS NULL
            ALTER TABLE Countries ADD Currency nvarchar(20) NULL;
        IF COL_LENGTH('Contents', 'CuisineId') IS NULL
            ALTER TABLE Contents ADD CuisineId uniqueidentifier NULL;
        IF OBJECT_ID(N'ContentCountries', N'U') IS NULL
        BEGIN
            CREATE TABLE ContentCountries (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ContentCountries PRIMARY KEY,
                ContentId uniqueidentifier NOT NULL,
                CountryId uniqueidentifier NOT NULL,
                Name nvarchar(400) NULL,
                Description nvarchar(max) NULL,
                Price decimal(18,3) NOT NULL CONSTRAINT DF_ContentCountries_Price DEFAULT 0,
                PriceQuantity decimal(18,3) NOT NULL CONSTRAINT DF_ContentCountries_PriceQuantity DEFAULT 1,
                PriceUnitId uniqueidentifier NULL,
                IsDeleted bit NOT NULL CONSTRAINT DF_ContentCountries_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_ContentCountries_Status DEFAULT 0
            );
            CREATE INDEX IX_ContentCountries_Country ON ContentCountries(CountryId, ContentId);
        END
        IF COL_LENGTH('ItemCountries', 'OwnRecipe') IS NULL
            ALTER TABLE ItemCountries ADD OwnRecipe bit NOT NULL CONSTRAINT DF_ItemCountries_OwnRecipe DEFAULT 0;
        IF COL_LENGTH('ItemContents', 'CountryId') IS NULL
            ALTER TABLE ItemContents ADD CountryId uniqueidentifier NULL;
        IF COL_LENGTH('ItemSteps', 'CountryId') IS NULL
            ALTER TABLE ItemSteps ADD CountryId uniqueidentifier NULL;
        IF COL_LENGTH('ItemVideos', 'CountryId') IS NULL
            ALTER TABLE ItemVideos ADD CountryId uniqueidentifier NULL;
        IF OBJECT_ID(N'ItemParts', N'U') IS NOT NULL AND COL_LENGTH('ItemParts', 'CountryId') IS NULL
            ALTER TABLE ItemParts ADD CountryId uniqueidentifier NULL;
        IF OBJECT_ID(N'RequestItems', N'U') IS NOT NULL AND COL_LENGTH('RequestItems', 'CountryId') IS NULL
            ALTER TABLE RequestItems ADD CountryId uniqueidentifier NULL;
        -- Old customer edits belong to the customer's own country version.
        IF OBJECT_ID(N'UserItemContents', N'U') IS NOT NULL AND COL_LENGTH('UserItemContents', 'CountryId') IS NULL
        BEGIN
            ALTER TABLE UserItemContents ADD CountryId uniqueidentifier NULL;
            EXEC(N'UPDATE e SET CountryId = u.CountryId FROM UserItemContents e JOIN Users u ON u.Id = e.UserId');
        END
        IF OBJECT_ID(N'UserItemSteps', N'U') IS NOT NULL AND COL_LENGTH('UserItemSteps', 'CountryId') IS NULL
        BEGIN
            ALTER TABLE UserItemSteps ADD CountryId uniqueidentifier NULL;
            EXEC(N'UPDATE e SET CountryId = u.CountryId FROM UserItemSteps e JOIN Users u ON u.Id = e.UserId');
        END
        IF OBJECT_ID(N'UserItemChoices', N'U') IS NOT NULL AND COL_LENGTH('UserItemChoices', 'CountryId') IS NULL
        BEGIN
            ALTER TABLE UserItemChoices ADD CountryId uniqueidentifier NULL;
            EXEC(N'UPDATE e SET CountryId = u.CountryId FROM UserItemChoices e JOIN Users u ON u.Id = e.UserId');
        END
        IF OBJECT_ID(N'UserContentPrices', N'U') IS NOT NULL AND COL_LENGTH('UserContentPrices', 'CountryId') IS NULL
        BEGIN
            ALTER TABLE UserContentPrices ADD CountryId uniqueidentifier NULL;
            EXEC(N'UPDATE e SET CountryId = u.CountryId FROM UserContentPrices e JOIN Users u ON u.Id = e.UserId');
        END
        -- Neighbor countries (comma separated ids): lists show the country first, then its neighbors.
        IF COL_LENGTH('Countries', 'Neighbors') IS NULL
            ALTER TABLE Countries ADD Neighbors nvarchar(max) NULL;
        IF COL_LENGTH('Countries', 'Neighbors') IS NOT NULL
        BEGIN
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111113,a1111111-1111-4111-8111-111111111112,a1111111-1111-4111-8111-111111111104,a1111111-1111-4111-8111-111111111105,a1111111-1111-4111-8111-111111111106'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111101''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111101,a1111111-1111-4111-8111-111111111112,a1111111-1111-4111-8111-111111111106'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111113''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111101,a1111111-1111-4111-8111-111111111113,a1111111-1111-4111-8111-11111111110a,a1111111-1111-4111-8111-11111111110b'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111112''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-11111111110b,a1111111-1111-4111-8111-111111111112'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-11111111110a''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-11111111110a,a1111111-1111-4111-8111-111111111109,a1111111-1111-4111-8111-111111111112,a1111111-1111-4111-8111-111111111116'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-11111111110b''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-11111111110b,a1111111-1111-4111-8111-111111111116'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111109''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111109,a1111111-1111-4111-8111-11111111110b'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111116''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111105,a1111111-1111-4111-8111-111111111101,a1111111-1111-4111-8111-111111111103,a1111111-1111-4111-8111-111111111102'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111104''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111104,a1111111-1111-4111-8111-111111111102,a1111111-1111-4111-8111-11111111110c,a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-111111111101'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111105''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111102,a1111111-1111-4111-8111-111111111104'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111103''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111103,a1111111-1111-4111-8111-111111111105,a1111111-1111-4111-8111-11111111110c,a1111111-1111-4111-8111-111111111104'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111102''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111102,a1111111-1111-4111-8111-111111111105,a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-111111111108'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-11111111110c''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111105,a1111111-1111-4111-8111-11111111110c,a1111111-1111-4111-8111-111111111108,a1111111-1111-4111-8111-11111111110f,a1111111-1111-4111-8111-111111111107,a1111111-1111-4111-8111-11111111110e,a1111111-1111-4111-8111-11111111110d,a1111111-1111-4111-8111-111111111111'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111106''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-11111111110c,a1111111-1111-4111-8111-111111111106'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111108''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-11111111110f'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111111''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-111111111111,a1111111-1111-4111-8111-111111111107'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-11111111110f''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-11111111110e,a1111111-1111-4111-8111-11111111110f'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111107''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111107,a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-11111111110d'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-11111111110e''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111106,a1111111-1111-4111-8111-11111111110e,a1111111-1111-4111-8111-111111111114,a1111111-1111-4111-8111-111111111115'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-11111111110d''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111115,a1111111-1111-4111-8111-11111111110d'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111114''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111114,a1111111-1111-4111-8111-11111111110d'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111115''');
            EXEC(N'UPDATE Countries SET Neighbors = ''a1111111-1111-4111-8111-111111111114,a1111111-1111-4111-8111-111111111115'' WHERE Neighbors IS NULL AND Id = ''a1111111-1111-4111-8111-111111111117''');
        END
        IF COL_LENGTH('Countries', 'Currency') IS NOT NULL
            EXEC(N'UPDATE Countries SET Currency = N''ج.م'' WHERE Currency IS NULL AND Id = ''a1111111-1111-4111-8111-111111111101''');
        -- Admin sort order (smaller first) for categories, groups, dishes, ingredients and places.
        IF COL_LENGTH('ItemCategories', 'Sort') IS NULL
            ALTER TABLE ItemCategories ADD Sort int NOT NULL CONSTRAINT DF_ItemCategories_Sort DEFAULT 0;
        IF COL_LENGTH('ItemGroups', 'Sort') IS NULL
            ALTER TABLE ItemGroups ADD Sort int NOT NULL CONSTRAINT DF_ItemGroups_Sort DEFAULT 0;
        IF COL_LENGTH('Items', 'Sort') IS NULL
            ALTER TABLE Items ADD Sort int NOT NULL CONSTRAINT DF_Items_Sort DEFAULT 0;
        IF COL_LENGTH('Contents', 'Sort') IS NULL
            ALTER TABLE Contents ADD Sort int NOT NULL CONSTRAINT DF_Contents_Sort DEFAULT 0;
        IF COL_LENGTH('PurchasePlaces', 'Sort') IS NULL
            ALTER TABLE PurchasePlaces ADD Sort int NOT NULL CONSTRAINT DF_PurchasePlaces_Sort DEFAULT 0;
        -- A dish in more than one category: the main one stays on Items.ItemCategoryId, the others are here.
        IF OBJECT_ID(N'ItemExtraCategories', N'U') IS NULL
        BEGIN
            CREATE TABLE ItemExtraCategories (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ItemExtraCategories PRIMARY KEY,
                ItemId uniqueidentifier NOT NULL,
                ItemCategoryId uniqueidentifier NOT NULL,
                IsDeleted bit NOT NULL CONSTRAINT DF_ItemExtraCategories_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_ItemExtraCategories_Status DEFAULT 0
            );
            CREATE INDEX IX_ItemExtraCategories_Item ON ItemExtraCategories(ItemId);
            CREATE INDEX IX_ItemExtraCategories_Category ON ItemExtraCategories(ItemCategoryId);
        END
        IF OBJECT_ID(N'Notifications', N'U') IS NULL
        BEGIN
            CREATE TABLE Notifications (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                Type nvarchar(40) NOT NULL,
                Title nvarchar(200) NULL,
                Body nvarchar(1000) NULL,
                Link nvarchar(400) NULL,
                Image nvarchar(400) NULL,
                CreatedAt datetime2 NOT NULL,
                ReadAt datetime2 NULL,
                [Key] nvarchar(200) NULL,
                IsDeleted bit NOT NULL CONSTRAINT DF_Notifications_IsDeleted DEFAULT 0,
                Status int NOT NULL CONSTRAINT DF_Notifications_Status DEFAULT 0
            );
            CREATE INDEX IX_Notifications_User_Created ON Notifications(UserId, CreatedAt DESC);
            CREATE UNIQUE INDEX IX_Notifications_User_Key ON Notifications(UserId, [Key]) WHERE [Key] IS NOT NULL;
        END

        DECLARE @LookupTables TABLE (Name sysname);
        INSERT INTO @LookupTables(Name) VALUES
            (N'ItemCategories'), (N'ItemGroups'), (N'ContentCategories'), (N'ContentGroup'),
            (N'PurchasePlaces'), (N'Countries'), (N'Units'), (N'Cuisines'), (N'Meals');

        DECLARE @table sysname;
        DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @LookupTables;
        OPEN table_cursor;
        FETCH NEXT FROM table_cursor INTO @table;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF OBJECT_ID(@table, N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(@table, 'IconPath') IS NULL
                    EXEC(N'ALTER TABLE [' + @table + N'] ADD IconPath nvarchar(max) NULL');
                IF COL_LENGTH(@table, 'Color') IS NULL
                    EXEC(N'ALTER TABLE [' + @table + N'] ADD Color nvarchar(max) NULL');
                IF COL_LENGTH(@table, 'TextColor') IS NULL
                    EXEC(N'ALTER TABLE [' + @table + N'] ADD TextColor nvarchar(max) NULL');
            END
            FETCH NEXT FROM table_cursor INTO @table;
        END
        CLOSE table_cursor;
        DEALLOCATE table_cursor;
        """);
    await Foods.API.Seed.CuisineSeeder.EnsureAsync(db);
}
if (args.Contains("--notify"))
{
    // One-off run of the notification generator: dotnet run -- --notify
    using var scope = app.Services.CreateScope();
    var generator = scope.ServiceProvider.GetRequiredService<Foods.Application.Customer.Notifications.NotificationGenerator>();
    Console.WriteLine("notifications added " + await generator.RunAsync(DateTime.UtcNow, CancellationToken.None));
    return;
}
    if (args.Contains("--seed") || args.Contains("--menu") || args.Contains("--stats") || args.Contains("--tidy") || args.Contains("--align"))
    {
        using var scope = app.Services.CreateScope();
        var db = (FoodsDbContext)scope.ServiceProvider.GetRequiredService<IFoodsDbContext>();
        if (args.Contains("--stats"))
        {
            var items = await db.Items.CountAsync(e => !e.IsDeleted);
            var links = await db.ItemCountries.CountAsync(e => !e.IsDeleted);
            var primary = await db.ItemCountries.CountAsync(e => !e.IsDeleted && e.IsPrimary);
            var meals = await (
                from link in db.ItemMeals
                join meal in db.Meals on link.MealId equals meal.Id
                where !link.IsDeleted && !meal.IsDeleted
                group link by meal.Name into g
                select new { Name = g.Key, Count = g.Select(x => x.ItemId).Distinct().Count() }
            ).ToListAsync();
            Console.WriteLine("items " + items + " links " + links + " primary " + primary);
            foreach (var meal in meals)
                Console.WriteLine(meal.Name + " " + meal.Count);
            var named = await db.Items.CountAsync(e => !e.IsDeleted && e.Name != null && (e.Name.Contains("فطار") || e.Name.Contains("غداء") || e.Name.Contains("عشاء")));
            var duplicateNames = await db.Items.Where(e => !e.IsDeleted && e.Name != null).GroupBy(e => e.Name!).Where(g => g.Count() > 1).CountAsync();
            Console.WriteLine("names-with-meal-word " + named);
            Console.WriteLine("duplicate-names " + duplicateNames);
            return;
        }
        var root = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var seeder = new Foods.API.Seed.CatalogSeeder(db, root);
        if (args.Contains("--align")) await seeder.AlignRecipesAsync();
        else if (args.Contains("--menu") || args.Contains("--tidy"))
        {
            await seeder.MergeItemsAsync();
            if (args.Contains("--menu")) await seeder.FillMenuAsync();
        }
        else if (args.Contains("--images")) await seeder.RefreshImagesAsync();
        else await seeder.RunAsync();
        return;
    }
app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Angular");
app.UseHttpsRedirection();
app.UseAuthentication();  
app.UseAuthorization();
app.MapControllers();
// SPA fallback: Angular deep links (/meals/all, /cms/...) get index.html on refresh,
// but unknown paths under an API prefix (customer, Auth, files, Meal, ...) stay a normal 404.
HashSet<string>? apiPrefixes = null;
app.MapFallback("{*path:nonfile}", async context =>
{
    apiPrefixes ??= context.RequestServices.GetRequiredService<EndpointDataSource>().Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => e.RoutePattern.PathSegments.FirstOrDefault()?.Parts.FirstOrDefault())
        .OfType<Microsoft.AspNetCore.Routing.Patterns.RoutePatternLiteralPart>()
        .Select(p => p.Content)
        .Append("swagger")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var first = (context.Request.Path.Value ?? "").Trim('/').Split('/')[0];
    var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)
        || apiPrefixes.Contains(first) || !File.Exists(index))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(index);
});
app.HandlerException();
app.Run();