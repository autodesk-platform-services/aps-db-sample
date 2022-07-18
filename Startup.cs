using System;
using forge_viewer_db_properties.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Startup
{
	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public IConfiguration Configuration { get; }

	// This method gets called by the runtime. Use this method to add services to the container.
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers();
		var ForgeClientID = Configuration["FORGE_CLIENT_ID"];
		var ForgeClientSecret = Configuration["FORGE_CLIENT_SECRET"];
		var ForgeCallbackURL = Configuration["FORGE_CALLBACK_URL"];
		var ForgeBucket = Configuration["FORGE_BUCKET"]; // Optional
		var mongoDBConnectionString = Configuration["MONGODB_CON_STRING"];
		var mongoDBName = Configuration["MONGODG_DBNAME"];
		var mongoDBCollection = Configuration["MONGODB_COLLECTION"];
		var mongoDBProperties = Configuration["DB_PROPERTIES_NAMES"];
		if (string.IsNullOrEmpty(ForgeClientID) || string.IsNullOrEmpty(ForgeClientSecret) || string.IsNullOrEmpty(ForgeCallbackURL))
		{
			throw new ApplicationException("Missing required environment variables FORGE_CLIENT_ID, FORGE_CLIENT_SECRET, or FORGE_CALLBACK_URL.");
		}
		services.AddSingleton<ForgeService>(new ForgeService(ForgeClientID, ForgeClientSecret, ForgeCallbackURL, ForgeBucket));
		services.AddSingleton<DBService>(new DBService(mongoDBName, mongoDBCollection, mongoDBConnectionString, mongoDBProperties));
		services.AddSignalR(o =>
		{
			o.EnableDetailedErrors = true;
			o.MaximumReceiveMessageSize = 10240; // bytes
		});
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		app.UseDefaultFiles();
		app.UseStaticFiles();
		app.UseRouting();
		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
			endpoints.MapHub<DBHub>("/dbhub");
		});
	}
}