using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using demo.RestApi.Customization;
using demo.Services.Implementations;
using demo.Services.Interfaces;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Linq;
using TvMazeApi;

namespace demo.RestApi
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
                .AddMvcOptions(options =>
                {
                    //only application/json support minimal mediatypes
                    foreach (var fmt in options.OutputFormatters.ToArray().Where(w => !(w is JsonOutputFormatter)))
                    {
                        options.OutputFormatters.RemoveType(fmt.GetType());
                    }
                })
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new LowerCaseContractResolver());
            // Add swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "TVMaze api (Egberts chunky Fork)", Version = "v1" });
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "demo.RestApi.xml");
                c.IncludeXmlComments(filePath);
            });
            services.AddScoped<ITvShowService, TvShowService>();
            services.AddScoped<TvMazeClient> ();
          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var virtPath = default(string);
            if (env.IsDevelopment())
            {
                virtPath = "";
                app.UseDeveloperExceptionPage();              
            }
            else
            {
                virtPath = "/tvmaze";
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseStaticFiles();
            // setup swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {

                c.SwaggerEndpoint(virtPath + "/swagger/v1/swagger.json", "v1");
               
            });

           

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
