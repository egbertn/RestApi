using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace demo.RestApi
{
         ///<summary>
     ///lah di dah
     ///</summary>
    public class Program
    {
             ///<summary>
     ///lah di dah
     ///</summary>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }
     ///<summary>
     ///lah di dah
     ///</summary>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseApplicationInsights();
    }
}