
using CloudInvoice.Billing.Application.Interfaces;
using CloudInvoice.Billing.Application.Mappings;
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
using AutoMapper;

namespace CloudInvoice.Billing.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddSingleton(sp =>
                new MapperConfiguration(
                    config => config.AddProfile<BillingMappingProfile>(),
                    sp.GetRequiredService<ILoggerFactory>()));
            builder.Services.AddSingleton<IMapper>(sp =>
                new Mapper(
                    sp.GetRequiredService<MapperConfiguration>(),
                    sp.GetRequiredService));

            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<ICatalogIntegrationService, CatalogIntegrationService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();




            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Insere o token JWT desta forma: Bearer {o_teu_token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });


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

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });


            var jwtSecret = builder.Configuration["JwtSettings:Secret"];
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; 
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"], 

                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtSettings:Audience"], 

                    ValidateLifetime = true 
                };
            });

            builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();


            var catalogApiUrl = builder.Configuration["ApiUrls:CatalogApi"];

            builder.Services.AddHttpClient<ICatalogIntegrationService, CatalogIntegrationService>(client =>
            {
                client.BaseAddress = new Uri(catalogApiUrl);

                client.Timeout = TimeSpan.FromSeconds(10);
            });

            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<CloudInvoice.Billing.Infrastructure.Data.ApplicationDbContext>();
                    await CloudInvoice.Billing.Infrastructure.Data.CustomerSeeder.SeedAsync(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Ocorreu um erro ao popular a base de dados com o seeder de clientes.");
                }
            }

            ApplicationDbSeeder.SeedAsync(app.Services).GetAwaiter().GetResult();


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
