using EnglishTeacher.Controllers;
using EnglishTeacher.Services.Assistants;
using EnglishTeacher.Services.ChatCompletions;
using EnglishTeacher.Services.Database;
using EnglishTeacher.Services.Threads;
using EnglishTeacher.Services;
using EnglishTeacher.Services.Plugins;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EnglishTeacher
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"]; 

            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient();

            // --- Semantic Kernel + Groq ---
            var groqApiKey = builder.Configuration["Groq:ApiKey"]
                ?? throw new InvalidOperationException("Groq:ApiKey não configurado. Use user-secrets ou variável de ambiente.");
            var groqModel = builder.Configuration["Groq:Model"] ?? "llama-3.1-8b-instant";

#pragma warning disable SKEXP0010
            builder.Services.AddKernel()
                .AddOpenAIChatCompletion(
                    modelId: groqModel,
                    apiKey: groqApiKey,
                    endpoint: new Uri("https://api.groq.com/openai/v1"));
#pragma warning restore SKEXP0010

            // Plugins SK: scoped para receber IHttpContextAccessor com o usuário autenticado.
            // São injetados diretamente no ChatCompletionsService — sem sobrescrever o Kernel no DI.
            builder.Services.AddScoped<ProgressPlugin>();
            builder.Services.AddScoped<ExercisePlugin>();
            // --- fim Semantic Kernel ---

            builder.Services.AddScoped<ITeacherService, TeacherService>();
            builder.Services.AddScoped<IThreadService, ThreadService>();
            builder.Services.AddScoped<IChatCompletionsService, ChatCompletionsService>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddScoped<SqlUserService>();
            builder.Services.AddScoped<SqlChatHistoryService>();
            builder.Services.AddScoped<SqlProgressService>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddHttpClient<TeacherController>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero 
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Headers["Authorization"]
                            .FirstOrDefault()?.Split(" ").Last();

                        if (string.IsNullOrEmpty(token))
                        {
                            token = context.HttpContext.Session.GetString("JwtToken");
                        }

                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                            context.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                            context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var response = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                message = "N�o autorizado. Fa�a login novamente.",
                                redirectUrl = "/Login/Login"
                            });
                            return context.Response.WriteAsync(response);
                        }

                        context.Response.Redirect("/Login/Login");
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax; 
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "AssistIA API",
                    Version = "v1",
                    Description = "API para Assistente IA com autentica��o por Cookies"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT no campo abaixo. Exemplo: Bearer {token}"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseSession(); 
            app.UseCors("AllowAll");
            app.UseAuthentication(); 
            app.UseAuthorization(); 

            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 401 && !context.Response.HasStarted)
                {
                    if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                        context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.ContentType = "application/json";
                        var response = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "Sess�o expirada. Redirecionando para login...",
                            redirectUrl = "/Login/Login"
                        });
                        await context.Response.WriteAsync(response);
                    }
                    else
                    {
                        context.Response.Redirect("/Login/Login");
                    }
                }
            });

            app.UseAppEndpoints();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Login}/{id?}");

            app.Run();
        }
    }
}

