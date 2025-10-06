using System;
using System.IO;
using System.Linq;
using AIDungeonPrompts.Application;
using AIDungeonPrompts.Backup.Persistence;
using AIDungeonPrompts.Domain;
using AIDungeonPrompts.Infrastructure;
using AIDungeonPrompts.Infrastructure.Identity;
using AIDungeonPrompts.Persistence;
using AIDungeonPrompts.Persistence.DbContexts;
using AIDungeonPrompts.Web.Constants;
using AIDungeonPrompts.Web.HostedServices;
using AIDungeonPrompts.Web.Middleware;
using AIDungeonPrompts.Web.ModelMetadataDetailsProviders;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation.AspNetCore;
using MediatR;
using MediatR.Extensions.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using NWebsec.AspNetCore.Mvc.Csp;
using Serilog;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace AIDungeonPrompts.Web
{
	public class Startup
	{
		private const string DatabaseConnectionName = ConfigurationConstants.DatabaseConnectionName;

		public Startup(IConfiguration configuration, IWebHostEnvironment environment)
		{
			Configuration = configuration;
			Environment = environment;
		}

		public IConfiguration Configuration { get; }
		public IWebHostEnvironment Environment { get; }

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseForwardedHeaders();

		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		else
		{
			// Use generic error page in production - does not expose stack traces or sensitive information
			app.UseExceptionHandler("/Home/Error");
			
			// Ensure status code pages don't leak information
			app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
		}

		// Enhanced HSTS with includeSubDomains and preload
		app.UseHsts(options => options
			.MaxAge(days: 365)
			.IncludeSubdomains()
			.Preload());
		app.UseXContentTypeOptions();
		app.UseXfo(options => options.Deny());
		app.UseHttpsRedirection();
		app.UseXXssProtection(options => options.EnabledWithBlockMode());
		app.UseReferrerPolicy(opts => opts.NoReferrer());
		
		// Additional security headers
		app.Use(async (context, next) =>
		{
			context.Response.Headers.Add("X-Download-Options", "noopen");
			context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
			await next();
		});

			app.UseStatusCodePages();

		app.UseCookiePolicy(new CookiePolicyOptions
		{
			HttpOnly = HttpOnlyPolicy.Always,
			Secure = CookieSecurePolicy.SameAsRequest, // Allow HTTP in non-HTTPS environments
				MinimumSameSitePolicy = SameSiteMode.Strict
			});

			app.UseCorrelationId();
			app.UseSerilogRequestLogging();

			var provider = new FileExtensionContentTypeProvider();
			provider.Mappings[".db"] = "application/octet-stream";
			app.UseStaticFiles(new StaticFileOptions
			{
				ContentTypeProvider = provider,
				OnPrepareResponse = context =>
				{
					ResponseHeaders? headers = context.Context.Response.GetTypedHeaders();
					headers.CacheControl = new CacheControlHeaderValue {MaxAge = TimeSpan.FromDays(1)};
				}
			});

			app.UseRouting();

			app.UseCors();
			app.UseAuthentication();
			app.UseAuthorization();

			app.UseMiddleware<CurrentUserMiddleware>();
			app.UseMiddleware<HoneyMiddleware>();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");
			});
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy("AiDungeon",
					builder =>
					{
						builder
							.WithOrigins("https://play.aidungeon.io")
							.WithMethods("GET");
					});
			});
			services.Configure<ForwardedHeadersOptions>(options =>
			{
				options.ForwardedHeaders =
					ForwardedHeaders.XForwardedFor |
					ForwardedHeaders.XForwardedProto |
					ForwardedHeaders.XForwardedHost;
			});

		services
			.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(builder =>
			{
				builder.LoginPath = "/user/login";
				builder.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP cookies
			});

			// See: https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-5.0&tabs=visual-studio#entity-framework-core
			services.AddDataProtection()
				.PersistKeysToDbContext<AIDungeonPromptsDbContext>();

		services
			.AddApplicationLayer()
			.AddPersistenceLayer(GetDatabaseConnectionString())
			.AddBackupPersistenceLayer(BackupDatabaseConnectionName())
			.AddInfrastructureLayer()
			.AddHttpContextAccessor()
			.AddDefaultCorrelationId()
			.AddDistributedMemoryCache()
			.AddMediatR(new[] {typeof(DomainLayer), typeof(ApplicationLayer)}.Select(t => t.Assembly).ToArray())
			.AddFluentValidation(new[] {typeof(ApplicationLayer)}.Select(t => t.Assembly).ToArray())
			.AddRouting(builder =>
			{
				builder.LowercaseUrls = true;
				builder.LowercaseQueryStrings = true;
			})
				.AddControllersWithViews(builder =>
				{
					builder.Filters.Add(typeof(CspAttribute));
					builder.Filters.Add(new CspDefaultSrcAttribute {Self = true});
					builder.Filters.Add(new CspImgSrcAttribute {Self = true, CustomSources = "data:"});
					builder.Filters.Add(new CspScriptSrcAttribute
					{
						Self = true, UnsafeEval = false, UnsafeInline = false
					});
					builder.Filters.Add(new CspStyleSrcAttribute {Self = true, UnsafeInline = false});
					builder.Filters.Add(new CspObjectSrcAttribute {None = true});
					builder.ModelMetadataDetailsProviders.Add(
						new DoNotConvertEmptyStringToNullMetadataDetailsProvider());
				})
				.AddFluentValidation(fv =>
				{
					fv.ImplicitlyValidateChildProperties = true;
					fv.RegisterValidatorsFromAssemblies(new[] {typeof(ApplicationLayer), typeof(Startup)}
						.Select(t => t.Assembly).ToArray());
				});

		services.AddAuthorization(options =>
		{
			options.AddPolicy(
				PolicyValueConstants.EditorsOnly,
				policy => policy.RequireClaim(ClaimValueConstants.CanEdit, true.ToString())
			);
			options.AddPolicy(
				PolicyValueConstants.AdminsOnly,
				policy => policy.RequireClaim(ClaimValueConstants.IsAdmin, true.ToString())
			);
		});

		// Configure request size limits
		services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
		{
			options.Limits.MaxRequestBodySize = ConfigurationConstants.MaxRequestBodySizeBytes;
		});

		services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
		{
			options.MultipartBodyLengthLimit = ConfigurationConstants.MaxFileUploadSizeBytes;
			options.ValueLengthLimit = ConfigurationConstants.MaxFileUploadSizeBytes;
		});

			services.AddHostedService<DatabaseMigrationHostedService>();
			services.AddHostedService<DatabaseBackupHostedService>();
			services.AddHostedService<DatabaseBackupCronJob>();
			services.AddHostedService<ApplicationLogCleanerCronJob>();
			services.AddHostedService<ReportCleanerCronJob>();
		}

	private string BackupDatabaseConnectionName() =>
		$"Data Source=/AIPromptDossier/backups/backup.db;";

	private string GetDatabaseConnectionString()
	{
		var connectionString = Configuration.GetConnectionString(DatabaseConnectionName);
		
		// Validate that connection string is configured
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException(
				$"Database connection string '{DatabaseConnectionName}' is not configured. " +
				"Please ensure appsettings.json or environment variables are properly configured.");
		}
		
		// Connection string is now fully configured via environment variables in docker-compose.yml
		// No need to add password here anymore
		
		return connectionString;
	}

}
}
