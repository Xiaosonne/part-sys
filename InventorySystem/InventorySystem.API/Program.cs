using System.Text;
using System.Text.Json;
using InventorySystem.API.Configuration;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Repositories;
using InventorySystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Serilog;

// 保存进程 ID 到文件
var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
var pidFilePath = Path.Combine(AppContext.BaseDirectory, "process.pid");
File.WriteAllText(pidFilePath, processId.ToString());
Console.WriteLine($"[STARTUP] Process ID: {processId}, saved to {pidFilePath}");

// 配置 Serilog
var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(logsPath, $"app-{processId}-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("应用启动中...");

    var builder = WebApplication.CreateBuilder(args);

    // 添加 Serilog
    builder.Host.UseSerilog();

    // 配置 MongoDB 序列化器处理 JsonElement
    var jsonElementSerializer = new JsonElementSerializer();
    BsonSerializer.RegisterSerializer(jsonElementSerializer);

    // Configuration
    builder.Services.Configure<WorkspaceOptions>(builder.Configuration.GetSection(WorkspaceOptions.SectionName));

    // MongoDB
    var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
    var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "inventory_db";
    var mongoClient = new MongoClient(mongoConnectionString);
    var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);
    builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

    // Repositories
    builder.Services.AddScoped<IPartRepository, PartRepository>();
    builder.Services.AddScoped<ISpecTemplateRepository, SpecTemplateRepository>();
    builder.Services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
    builder.Services.AddScoped<IProjectNodeRepository, ProjectNodeRepository>();
    builder.Services.AddScoped<ISelectionPlanRepository, SelectionPlanRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
    builder.Services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
    builder.Services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
    builder.Services.AddScoped<IWorkflowTaskRepository, WorkflowTaskRepository>();
    builder.Services.AddScoped<IWorkflowHistoryRepository, WorkflowHistoryRepository>();
    builder.Services.AddScoped<IWorkspaceStructureRepository, WorkspaceStructureRepository>();
    builder.Services.AddScoped<IPartCategoryRepository, PartCategoryRepository>();
    builder.Services.AddScoped<IPurchaseTaskRepository, PurchaseTaskRepository>();

    // Services
    builder.Services.AddScoped<IStockService, StockService>();
    builder.Services.AddScoped<ISelectionService, SelectionService>();
    builder.Services.AddScoped<IWorkflowService, WorkflowService>();
    builder.Services.AddScoped<IWorkflowTaskService, WorkflowTaskService>();
    builder.Services.AddSingleton<IFileStorageService>(new LocalFileStorageService(Path.Combine(AppContext.BaseDirectory, "wwwroot/files")));
    builder.Services.AddScoped<IWorkspaceInitializer>(sp =>
    {
        var fileService = sp.GetRequiredService<IFileStorageService>();
        var workspaceStructureRepo = sp.GetRequiredService<IWorkspaceStructureRepository>();
        var configPath = Path.Combine(AppContext.BaseDirectory, "project-workspace-structure.json");
        return new WorkspaceInitializer(fileService, workspaceStructureRepo, configPath);
    });

    // JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "default-dev-secret-key-change-in-production";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Inventory System API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header. Format: Bearer {token}",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Seed admin user if none exists
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000);
        await SeedAdminUser(app);
    });

    Log.Information("应用启动完成，监听端口 5000");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用启动失败");
}
finally
{
    Log.CloseAndFlush();
}

static async Task SeedAdminUser(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var existing = await userRepo.GetByUsernameAsync("admin");
        if (existing == null)
        {
            await userRepo.CreateAsync(new InventorySystem.Core.Models.User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                DisplayName = "管理员",
                Role = "admin"
            });
            Log.Information("已创建默认管理员账户: admin / admin123");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "无法连接到 MongoDB 或创建管理员账户");
    }
}

// MongoDB JsonElement 序列化器
public class JsonElementSerializer : IBsonSerializer<JsonElement>
{
    public Type ValueType => typeof(JsonElement);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonElement value)
    {
        var writer = context.Writer;

        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                writer.WriteNull();
                break;
            case JsonValueKind.True:
                writer.WriteBoolean(true);
                break;
            case JsonValueKind.False:
                writer.WriteBoolean(false);
                break;
            case JsonValueKind.Number:
                if (value.TryGetInt32(out var intValue))
                    writer.WriteInt32(intValue);
                else if (value.TryGetInt64(out var longValue))
                    writer.WriteInt64(longValue);
                else if (value.TryGetDouble(out var doubleValue))
                    writer.WriteDouble(doubleValue);
                else
                    writer.WriteString(value.GetRawText());
                break;
            case JsonValueKind.String:
                writer.WriteString(value.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                {
                    Serialize(context, args, item);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Object:
                writer.WriteStartDocument();
                foreach (var property in value.EnumerateObject())
                {
                    writer.WriteName(property.Name);
                    Serialize(context, args, property.Value);
                }
                writer.WriteEndDocument();
                break;
            default:
                writer.WriteString(value.GetRawText());
                break;
        }
    }

    public JsonElement Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var bsonType = reader.GetCurrentBsonType();

        return bsonType switch
        {
            BsonType.Null => JsonSerializer.Deserialize<JsonElement>("null"),
            BsonType.Boolean => JsonSerializer.Deserialize<JsonElement>(reader.ReadBoolean().ToString().ToLower()),
            BsonType.Int32 => JsonSerializer.Deserialize<JsonElement>(reader.ReadInt32().ToString()),
            BsonType.Int64 => JsonSerializer.Deserialize<JsonElement>(reader.ReadInt64().ToString()),
            BsonType.Double => JsonSerializer.Deserialize<JsonElement>(reader.ReadDouble().ToString()),
            BsonType.String => JsonSerializer.Deserialize<JsonElement>($"\"{reader.ReadString()}\""),
            _ => JsonSerializer.Deserialize<JsonElement>("null")
        };
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }

    void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is JsonElement jsonElement)
        {
            Serialize(context, args, jsonElement);
        }
    }
}
