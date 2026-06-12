using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using ask.ContextDb;
using ask.Dtos.General;
using ask.Implementation;
using ask.Interface;
using ask.Services;
using InteroperabiliteProject.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OracleApi.Services;
using Serilog;


namespace ask
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownProxies.Add(IPAddress.Parse("10.0.0.100"));
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardedHostHeaderName = "X-Forwarded-Host";
                options.ForwardedProtoHeaderName = "X-Forwarded-Proto";
            });


            var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                        ValidAudience = builder.Configuration["JwtSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });



            builder.Services.AddAuthorization();
            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                // Configuration de la sécurité JWT dans Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            builder.Services.AddDbContext<askContext>(options => 
                options.UseMySql(
                    builder.Configuration.GetConnectionString("MySqlConnection"),
                    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySqlConnection"))
                ));
            builder.Services.AddDbContextFactory<askContext>(options => 
                options.UseMySql(
                    builder.Configuration.GetConnectionString("MySqlConnection"),
                    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MySqlConnection"))
                ), ServiceLifetime.Scoped);
            builder.Services.AddCors(o =>
           o.AddPolicy
               ("Stockpolicie", b =>
               {
                   b.AllowAnyOrigin();
                   b.AllowAnyMethod();
                   b.AllowAnyHeader();
               }
               )
           );

            builder.Services.Configure<ParamAsaci>(builder.Configuration.GetSection("Asaci"));
            builder.Services.Configure<ParamMessage>(builder.Configuration.GetSection("Messagerie"));
            builder.Services.Configure<SecurityConfig>(builder.Configuration.GetSection("security"));

            builder.Services.AddScoped<IHistoSmsRepo, HistoSmsRepo>();
            builder.Services.AddScoped<IHistoEmailRepo, HistoEmailRepo>();

            builder.Services.AddScoped<ImodeleRepo, ModeleRepo>();
            builder.Services.AddScoped<ServiceAsaci>();
            builder.Services.AddScoped<ServiceMessagerie>();
            builder.Services.AddScoped<UserValidationService>();
            builder.Services.AddScoped<IUserRepo, UserRepo>();
            builder.Services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
            builder.Services.AddScoped<JwtService>();
            builder.Services.AddScoped<IOracleService, OracleService>();


            builder.Services.AddHttpClient();
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .WriteTo.Console() // Pour la sortie console
                .WriteTo.File("/log/print-attestation/log-.txt", rollingInterval: RollingInterval.Day) // Pour la sortie fichier
                .CreateLogger();
            // Remplacer le logger par défaut par Serilog
            builder.Host.UseSerilog();

            //************************************SERILOG************************************************************

            //***********************************ajouter Auto Mapper*****************************************
            // Ajouter les services AutoMapper
            builder.Services.AddAutoMapper(typeof(Program));
            //***********************************ajouter Auto Mapper*****************************************



            builder.Services.AddRateLimiter(options =>
            {
                var RateLimiterSetting = builder.Configuration.GetSection("security:RateLimiter");
                int tokensPerPeriod = int.TryParse(RateLimiterSetting["tokensPerPeriod"], out var value) ? value : 5;
                int tokenLimit = int.TryParse(RateLimiterSetting["tokenLimit"], out var values) ? values : 5;
                int minutes = int.TryParse(RateLimiterSetting["minutes"], out var minute) ? minute : 1;

                // Définition d'une policy nommée "MobilePolicy"
                options.AddPolicy("MobilePolicy", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = tokenLimit,
                        TokensPerPeriod = tokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(minutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                });

                // code HTTP renvoyé quand bloqué
                options.RejectionStatusCode = 429;
            });



            var app = builder.Build();

            // Stockage du ServiceProvider global
            //// Appliquer automatiquement les migrations au démarrage
            //using (var scope = app.Services.CreateScope())
            //{
            //    var dbContext = scope.ServiceProvider.GetRequiredService<InteropContext>();
            //    dbContext.Database.Migrate(); // Cette ligne applique les migrations automatiquement
            //}

            app.UseStaticFiles();
            app.UseForwardedHeaders();

            //***********************************Middleware interceptor***********************
            //app.UseMiddleware<InterceptRequest>();
            //***********************************Middleware interceptor***********************


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            //******************************************Modifier le server qui apparait dans la reponse pour la securité************************************
            app.Use(async (context, next) =>
            {
                //context.Response.Headers.Remove("x-powered-by");
                context.Response.Headers["server"] = "XXX-XXX-XXX";
                await next();

            });

            // Middleware pour gérer les erreurs 404
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/json";

                    var problem = GeneraleRetour.BuildNotFound(
                                     detail: "L'url n'a pas pu être contacté",
                                     instance: context.Request.Path
                                 );

                    var json = JsonConvert.SerializeObject(problem);
                    await context.Response.WriteAsync(json);
                }
            });
            // Middleware pour gérer les erreurs 404

            //******************************************Modifier le server qui apparait dans la reponse pour la securité************************************


            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == 429)
                    {
                        context.Response.ContentType = "application/json";
                        var response = JsonConvert.SerializeObject(GeneraleRetour.BuildProblemResponse429(
                            instance: context.Request.Path
                        ));
                        return context.Response.WriteAsync(response);
                    }
                    return Task.CompletedTask;
                });

                await next();
            });


            app.UseCors("Stockpolicie");
            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseMiddleware<JwtSecureMiddleware>();


            app.UseAuthorization();
            //*******************************************TEST ENCODAG DES CARACTERE POUR LINUX**************************************
            //app.Use(async (context, next) =>
            //{
            //    context.Request.Headers["Accept-Charset"] = "utf-8";

            //    if (string.IsNullOrEmpty(context.Request.Headers["Content-Type"]))
            //        context.Request.Headers["Content-Type"] = "application/json; charset=utf-8";

            //    await next();
            //});

            //*******************************************TEST ENCODAG DES CARACTERE POUR LINUX**************************************

            //using (var scope = app.Services.CreateScope())
            //{
            //    var dbContext = scope.ServiceProvider.GetRequiredService<InteropContext>();

            //    // Crée la base si elle n'existe pas
            //    dbContext.Database.Migrate();

            //}
            app.MapControllers().RequireRateLimiting("MobilePolicy"); ;
            //app.MapControllers() ;
            //   app.UseMiddleware<TraceMidleware>();
            app.Run("http://*:5002");
        }
    }
}
