using System;
using aps_viewer_db_properties.Hubs;
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
    services.AddControllers().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Latest).AddNewtonsoftJson();
    var APSClientID = Configuration["APS_CLIENT_ID"];
    var APSClientSecret = Configuration["APS_CLIENT_SECRET"];
    var APSCallbackURL = Configuration["APS_CALLBACK_URL"];
    var APSBucket = Configuration["APS_BUCKET"]; // Optional
    var mongoDBConnectionString = Configuration["MONGODB_CON_STRING"];
    var mongoDBName = Configuration["MONGODB_DBNAME"];
    var mongoDBCollection = Configuration["MONGODB_COLLECTION"];
    var mongoDBProperties = Configuration["DB_PROPERTIES_NAMES"];
    if (string.IsNullOrEmpty(APSClientID) || string.IsNullOrEmpty(APSClientSecret) || string.IsNullOrEmpty(APSCallbackURL))
    {
      throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, or APS_CALLBACK_URL.");
    }
    services.AddSingleton<APSService>(new APSService(APSClientID, APSClientSecret, APSCallbackURL, APSBucket));
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