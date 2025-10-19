using HypnoTools.API.Data;
using HypnoTools.API.Repositories;
using HypnoTools.API.Services.Auth;
using HypnoTools.API.Services.ERP;
using HypnoTools.API.Services.ImportacaoProduto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       "Server=localhost;Database=HypnoToolsDB;User=root;Password=;";
builder.Services.AddDbContext<HypnoToolsDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add Repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Add HTTP Client and Context Accessor
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Configurar HttpClient com timeout estendido para o ERPIntegrationService
builder.Services.AddHttpClient<IERPIntegrationService, ERPIntegrationService>(client =>
{
    // Aumentar timeout para 5 minutos (útil para respostas grandes)
    client.Timeout = TimeSpan.FromMinutes(5);
    // Aumentar tamanho máximo de buffer de conteúdo (100MB)
    client.MaxResponseContentBufferSize = 100 * 1024 * 1024;
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Habilitar compressão automática
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// Add custom services
builder.Services.AddScoped<IHypnoCoreAuthService, HypnoCoreAuthService>();
// ERPIntegrationService já registrado acima com HttpClient configurado
builder.Services.AddScoped<IImportacaoProdutoService, ImportacaoProdutoService>();

// Add JWT Authentication - Usar a mesma chave do HypnoCore
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "f-@,D-p$^2$&C?KOqRL1/xuoObWO<Q6T{n{1@dS*Gas<!x1OZ2pN!i}E,?wCb4L?";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Evento para logs de debug
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT token validated successfully for user: {User}",
                    context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // Política mais permissiva para desenvolvimento
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS deve vir antes de outras middlewares
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("AllowAngularDev");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HypnoToolsDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
