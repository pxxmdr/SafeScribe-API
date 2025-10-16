using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SafeScribe.Api.Data;
using SafeScribe.Api.Middlewares;
using SafeScribe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SafeScribe API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Cole apenas o token JWT (sem 'Bearer ').",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("safescribe-db"));

var configuration = builder.Configuration;
var jwtIssuer   = configuration["Jwt:Issuer"];
var jwtAudience = configuration["Jwt:Audience"];
var jwtSecret   = configuration["Jwt:Secret"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ValidateIssuer:
            //  - Ativa a validação do emissor do token (claim 'iss').
            //  - Garante que o token só é aceito se tiver sido emitido pelo emissor esperado.
            ValidateIssuer = true,

            // ValidIssuer:
            //  - Define qual emissor é considerado válido.
            //  - Deve corresponder ao valor configurado ao gerar o token (ex.: "SafeScribe").
            ValidIssuer = jwtIssuer,

            // ValidateAudience:
            //  - Ativa a validação da audiência (claim 'aud').
            //  - Garante que o token foi emitido para esta API específica.
            ValidateAudience = true,

            // ValidAudience:
            //  - Define a audiência aceita por esta API.
            //  - Deve coincidir com a audiência usada na emissão do token (ex.: "SafeScribe.Api").
            ValidAudience = jwtAudience,

            // ValidateIssuerSigningKey:
            //  - Exige que a assinatura criptográfica do token seja verificada.
            //  - Garante integridade e autenticidade (o token não foi alterado).
            ValidateIssuerSigningKey = true,

            // IssuerSigningKey:
            //  - Chave usada para validar a assinatura do token (no caso, simétrica).
            //  - Deve ser mantida em segredo e ter tamanho adequado (32+ caracteres para HMAC-SHA256).
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),

            // ValidateLifetime:
            //  - Habilita a checagem de expiração (claim 'exp') e de "não antes de" (claim 'nbf').
            //  - Impede o uso de tokens expirados ou ainda não válidos.
            ValidateLifetime = true,

            // ClockSkew:
            //  - Janela de tolerância para diferenças de relógio entre sistemas.
            //  - Evita falsos negativos próximos do limite de expiração.
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<ITokenBlacklistService, InMemoryTokenBlacklistService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
