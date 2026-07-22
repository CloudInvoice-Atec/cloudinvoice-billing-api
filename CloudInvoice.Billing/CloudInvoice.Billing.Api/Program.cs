
using CloudInvoice.Billing.Application.Interfaces;
using CloudInvoice.Billing.Application.Services;
using CloudInvoice.Billing.Domain.Interfaces;
using CloudInvoice.Billing.Infrastructure.Data;
using CloudInvoice.Billing.Infrastructure.Repositories;
using CloudInvoice.Billing.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CloudInvoice.Billing.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<ICatalogIntegrationService, CatalogIntegrationService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                // 1. Cria o botão "Authorize" e diz ao Swagger como o Token deve ser enviado
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Insere o token JWT desta forma: Bearer {o_teu_token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                // 2. Obriga o Swagger a enviar esse Token em todos os pedidos que fizeres
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                // Adiciona estas 3 linhas para ler os comentários XML:
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            // 1. Lê a chave secreta do appsettings.json e transforma-a em bytes
            var jwtSecret = builder.Configuration["JwtSettings:Secret"];
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            // 2. Configura o serviço de Autenticação
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Em desenvolvimento, podemos deixar false
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // O que é que a Billing.API vai exigir que o Token tenha para o aceitar?
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"], // Tem de ser o da porta 5001

                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtSettings:Audience"], // CloudInvoiceUsers

                    ValidateLifetime = true // Rejeita tokens que já passaram da validade
                };
            });

            // 1. Lemos o URL da Catalog API que tu configuraste no appsettings.json
            var catalogApiUrl = builder.Configuration["ApiUrls:CatalogApi"];

            builder.Services.AddHttpClient<ICatalogIntegrationService, CatalogIntegrationService>(client =>
            {
                client.BaseAddress = new Uri(catalogApiUrl);
                // Podes também configurar um Timeout para a chamada não ficar pendurada para sempre
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
