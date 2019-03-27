using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using rtl.RestApi.Customization;
using rtl.Services.Implementations;
using rtl.Services.Interfaces;
using Swashbuckle.AspNetCore.Swagger;
using TvMazeApi;

namespace rtl.RestApi
{
    //NOTE: using DBContext is too much weight for the scope. Consider it an option, but I use MemoryCache
    //NOTE2: For Model and TvMazeApiClient I used code from Erwin Beckers (why reinvent).
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;            
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new LowerCaseContractResolver());
            // Add swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "TVMaze api (Egberts chunky Fork)", Version = "v1" });
            });
            services.AddScoped<ITvShowService, TvShowService>();
            services.AddScoped<TvMazeClient> ();
          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();              
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseStaticFiles();
            // setup swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
