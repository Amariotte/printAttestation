using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Event;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Implementation;
using Newtonsoft.Json;
using InteroperabiliteProject.Controllers;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using InteroperabiliteProject.ServicceAIP;
using InteroperabiliteProject.ServicesKeycloack;
using InteroperabiliteProject.Dtos;
using Microsoft.IdentityModel.Tokens;
using InteroperabiliteProject.ServicesKeycloack.Dtos;
using System.Security.Authentication;
using InteroperabiliteProject.Tools;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using ask.Dtos.RequestToSendDto;
using ask.Services;


namespace InteroperabiliteProject
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

            var secure_method = builder.Configuration.GetValue<string>("security:secure_method");
            switch (secure_method)
            {

                case "KEYLOACK":

                    var keyloackSetting = builder.Configuration.GetSection("security:keycloack");
                    var keycloakAuthority = $"{keyloackSetting["Uri"]}realms/{keyloackSetting["realm"]}";

                    builder.Services.AddSingleton<TokenValidationParameters>(sp =>
                    {
                        return new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidIssuer = keycloakAuthority,
                            ValidateAudience = true,
                            ValidAudience = keyloackSetting["audience"],
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero,
                            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                            {

                                var handler = new HttpClientHandler()
                                {
                                    SslProtocols = System.Security.Authentication.SslProtocols.Ssl2 | SslProtocols.Tls13,
                                };

                                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                                using var client = new HttpClient(handler);

                                var jwks = client.GetStringAsync($"{keycloakAuthority}/protocol/openid-connect/certs").Result;
                                var keysContainer = JsonConvert.DeserializeObject<KeysContainer>(jwks);

                                Console.WriteLine(jwks);

                                var key = keysContainer.Keys.FirstOrDefault(k => k.Kid == kid);

                                if (key != null)
                                {
                                    var jsonWebKey = new JsonWebKey
                                    {
                                        Kty = key.Kty,
                                        Kid = key.Kid,
                                        E = key.E,
                                        N = key.N,
                                    };

                                    return new[] { jsonWebKey };
                                }
                                return null;
                            }
                        };
                    });

                    // Configure l'authentification et ajoute la personnalisation des retours d'erreur
                    builder.Services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        var validationParameters = builder.Services.BuildServiceProvider().GetRequiredService<TokenValidationParameters>();

                        options.Authority = keycloakAuthority;
                        options.RequireHttpsMetadata = keyloackSetting.GetValue<bool>(keyloackSetting["RequireHttpsMetadata"]);
                        options.Audience = keyloackSetting["audience"];
                        options.TokenValidationParameters = validationParameters;


                        options.Events = new JwtBearerEvents
                        {


                            OnChallenge = context =>
                            {
                                context.HandleResponse();
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var problem = GeneraleRetour.BuildUnauthorized(
                                    detail: "Le token est manquant ou invalide.",
                                    instance: context.Request.Path,
                                    invalidParams: new List<InvalidParam> { new InvalidParam { name = "Authorization", reason = "Le token d'accès est requis ou invalide." } }
                                    );

                                var result = JsonConvert.SerializeObject(problem);
                                return context.Response.WriteAsync(result);
                            },
                            OnForbidden = context =>
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                context.Response.ContentType = "application/json";


                                var problem = GeneraleRetour.BuildUnauthorized(
                                 detail: "Vous n'avez pas la permission d'accéder à cette ressource",
                                 instance: context.Request.Path
                                 );

                                var result = JsonConvert.SerializeObject(problem);

                                return context.Response.WriteAsync(result);
                            },

                            // Gérer l'expiration du token
                            OnTokenValidated = context =>
                            {
                                var expirationTime = context.SecurityToken.ValidTo;

                                if (expirationTime < DateTime.UtcNow)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                    context.Response.ContentType = "application/json";


                                    var problem = GeneraleRetour.BuildUnauthorized(
                                        detail: "Le token a expiré et n'est plus valide.",
                                        instance: context.Request.Path,
                                        invalidParams: new List<InvalidParam> { new InvalidParam { name = "Authorization", reason = "\"Le token a expiré et n'est plus valide." } }
                                    );

                                    var result = JsonConvert.SerializeObject(problem);
                                    return context.Response.WriteAsync(result);
                                }

                                return Task.CompletedTask;
                            },
                            OnAuthenticationFailed = context =>
                            {
                                context.NoResult();
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var problem = GeneraleRetour.BuildProblemResponse500(
                                 instance: context.Request.Path
                             );

                                var result = JsonConvert.SerializeObject(problem);


                                return context.Response.WriteAsync(result);
                            },
                        };
                    });


                    break;

            }


            builder.Services.AddAuthorization();
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<RouteNameFilter>();
            });



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
            //  builder.Services.AddDbContext<InteropContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PgConnection")), ServiceLifetime.Singleton);


            builder.Services.AddDbContext<InteropContext>(options =>options.UseNpgsql(builder.Configuration.GetConnectionString("PgConnection")));
            builder.Services.AddDbContextFactory<InteropContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("PgConnection")), ServiceLifetime.Scoped);
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

            //var test = builder.Configuration.GetSection("Aip");
            //var Aipdata = JsonConvert.DeserializeObject<AIPDATA>(builder.Configuration.GetSection("Aip").ToString());

            builder.Services.Configure<AIPDATA>(builder.Configuration.GetSection("Aip"));
            builder.Services.Configure<PARAM_MESSAGE>(builder.Configuration.GetSection("Messagerie"));
            builder.Services.Configure<SecurityConfig>(builder.Configuration.GetSection("security"));
            builder.Services.AddSingleton<EventService>();
            builder.Services.AddScoped<ServiceAuth>();
            builder.Services.AddScoped<KeycloackService>();
            builder.Services.AddScoped<SecureService>();
            builder.Services.AddScoped<ServiceAIF>();
            builder.Services.AddScoped<ServiceMessagerie>();
            builder.Services.AddScoped<ServiceAlias>();
            builder.Services.AddScoped<ServiceEtat>();
            builder.Services.AddScoped<ServiceTransfert>();
            builder.Services.AddScoped<ReceptionAIPController>();
            builder.Services.AddScoped<EnvoieController>();
            builder.Services.AddTransient<IemployeRepo, AliasRepo>();
            builder.Services.AddScoped<IscheduledRepo, ScheduledRepo>();
            builder.Services.AddScoped<IoperationmasseRepo, OperationMasseRepo>();
            builder.Services.AddScoped<IcompteRepo, CompteRepo>();
            builder.Services.AddScoped<IotpRepo, OtpRepo>();
            builder.Services.AddScoped<IHistoSmsRepo, HistoSmsRepo>();
            builder.Services.AddScoped<ICodeErreurRepo, CodeErreurRepo>();
            builder.Services.AddScoped<IParametreSystemeRepo, ParametreSystemeRepo>();
            builder.Services.AddScoped<ImodeleRepo,ModeleRepo>();
            builder.Services.AddScoped<IHistoEmailRepo, HistoEmailRepo>();
            builder.Services.AddScoped<ItransfertRepo, TransfertRepo>();
            builder.Services.AddScoped<ItransfertDispoRepo, TransfertDispoRepo>();
            builder.Services.AddScoped<IclientRepo, ClientRepo>();
            builder.Services.AddScoped<IcreationAliasRepo, creationAliasRepo>();
            builder.Services.AddScoped<ItransfertAutoriseRepo, TransfertAutoriseRepo>();
            builder.Services.AddScoped<ItransfertPlafondRepo, TransfertPlafondRepo>();
            builder.Services.AddScoped<IrevendicationRepo, RevendicationRepo>();
            builder.Services.AddScoped<IreferenceRepo, ReferenceRepo>();
            builder.Services.AddScoped<ITraceRepo, TraceRepo>();
            builder.Services.AddScoped<IDemandeRepo, DemandeRepo>();
            builder.Services.AddScoped<IRetourFondRepo, RetourFondRepo>();
            builder.Services.AddScoped<IdemandeLigneRepo, DemandeligneRepo>();
            builder.Services.AddScoped<IdatasRepo, DatasRepo>();
            builder.Services.AddTransient<IParticipantsRepo, ParticipantsRepo>();
            builder.Services.AddScoped<IwebhooksRepo, WebhooksRepo>();
            builder.Services.AddScoped<Iannulation_transfert, annulation_transfertBaseRepo>();
            builder.Services.AddScoped<InotificationRepo, NotificationRepo>();
            builder.Services.AddScoped<ClientValidationService>();

        
            //builder.Services.AddScoped<IaliasRepo, AliasRepo>();
            builder.Services.AddHttpClient();
            // Ajouter le service d'arrière-plan
            builder.Services.AddHostedService<TimedHostedService>();
            //*************************************Gestion des dates avec postgressSQL***************************************
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            //*************************************Gestion des dates avec postgressSQL***************************************
            //************************************SERILOG************************************************************
            //// Configure Serilog
            //builder.Host.UseSerilog((context, configuration) =>
            //                configuration.ReadFrom.Configuration(context.Configuration));

            // Configurer Serilog
            // Configurer Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .WriteTo.Console() // Pour la sortie console
                .WriteTo.File("/Journalisation/bceao/Naomi-.txt", rollingInterval: RollingInterval.Day) // Pour la sortie fichier
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

            switch (secure_method)
            {
                case "SECURE":
                    app.UseMiddleware<JwtSecureMiddleware>();
                    break;
                case "KEYLOACK":
                    app.UseMiddleware<JwtKeyloackMiddleware>();
                    break;
            }

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
            // Initialisation du logger statique
            RequettePI.Initialize(app.Services.GetRequiredService<ILoggerFactory>());
           app.MapControllers().RequireRateLimiting("MobilePolicy"); ;
            //app.MapControllers() ;
         //   app.UseMiddleware<TraceMidleware>();
            app.Run("http://*:5002");
        }
    }
}
