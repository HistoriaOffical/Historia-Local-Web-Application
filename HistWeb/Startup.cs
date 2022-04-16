using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HistWeb.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using reCAPTCHA.AspNetCore;
using Microsoft.Data.Sqlite;

namespace HistWeb
{
    public static class ApplicationSettings
    {
        /*
          	1. IPFS Host // This is IP Address - This could be publicly available IPFS system and not running on local machine
	        2. IPFS Port // This is a port number - This could be publicly available IPFS system and not running on local machine
	        3. Test Connection to IPFS Port // This is a button that verifies valid connection to a IPFS Host, can be done by request a IPFS file that exists on all IPFS hosts such as the help message.
	        ------
	        4. IPFS API Host // This is IP Address - User must set this up, running on local machine, as most IPFS systems do not have API access publicly allowed. (V1 won't use this)
	        5. IPFS API Port // This is a port number -  User must set this up, running on local machine, as most IPFS systems do not have API access publicly allowed (V1 won't use this)
	        6. Test Connection to IPFS API Port // This is a button that verifies valid connection to a IPFS API, can be done by calling a API function
	        --------
	        7. Historia Client // This is IP Address
	        8. Historia RPC Port // This is a port number
	        9. Historia RPC Username // This is a username
	        10. Historia RPC Password // This is a password.
        */
        public static string IPFSHost { get; set; }
        public static int IPFSPort { get; set; }
        public static string IPFSApiHost { get; set; }
        public static int IPFSApiPort { get; set; }
        public static string HistoriaClientIPAddress { get; set; }
        public static int HistoriaRPCPort { get; set; }
        public static string HistoriaRPCUserName { get; set; }
        public static string HistoriaRPCPassword { get; set; }

        private static void CreateConfig()
        {
            try
            {

                using (var connection = new SqliteConnection("Data Source=basex.db"))
                {
                    connection.Open();

                    using (var createCmd = connection.CreateCommand())
                    {
                        createCmd.CommandText = @"CREATE TABLE IF NOT EXISTS basexConfiguration (
                                                    Id INTEGER PRIMARY KEY,
                                                    IPFSHost TEXT  NOT NULL,
                                                    IPFSPort INTEGER NOT NULL,
                                                    IPFSApiHost TEXT NOT NULL,
                                                    IPFSApiPort INTEGER NOT NULL,
                                                    HistoriaClientIPAddress TEXT NOT NULL,
                                                    HistoriaRPCPort INTEGER NOT NULL,
                                                    HistoriaRPCUserName TEXT NOT NULL,
                                                    HistoriaRPCPassword TEXT NOT NULL
                                                );";
                        createCmd.CommandType = System.Data.CommandType.Text;
                        createCmd.ExecuteNonQuery();

                        createCmd.CommandText = @"CREATE TABLE IF NOT EXISTS masternodeprivatekeys (
                                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    collateralIndex TEXT  NOT NULL,
                                                    collateralHash TEXT NOT NULL,
                                                    masternodeName TEXT NOT NULL,
                                                    EncryptedPrivateKey TEXT
                                                );";
                        createCmd.CommandType = System.Data.CommandType.Text;
                        createCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED TO Create to base db or basexConfiguration table", ex.ToString());
            }
        }

        public static void SaveConfig()
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=basex.db"))
                {
                    connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        IPFSApiHost = "NONE";
                        IPFSApiPort = 0;
                        cmd.CommandText = "INSERT INTO basexConfiguration (Id, IPFSHost, IPFSPort, IPFSAPIPort, IPFSAPIHost, HistoriaClientIPAddress, HistoriaRPCPort, HistoriaRPCUserName, HistoriaRPCPassword)" +
"VALUES(1, @IPFSHost, @IPFSPort, @IPFSApiHost, @IPFSApiPort, @HistoriaClientIPAddress, @HistoriaRPCPort, @HistoriaRPCUserName, @HistoriaRPCPassword)" +
"ON CONFLICT(Id) DO UPDATE SET " +
"Id = 1, IPFSHost = @IPFSHost, IPFSPort = @IPFSPort, IPFSApiHost = @IPFSApiHost, IPFSApiPort = @IPFSApiPort, HistoriaClientIPAddress = @HistoriaClientIPAddress, " +
"HistoriaRPCPort = @HistoriaRPCPort, HistoriaRPCUserName = @HistoriaRPCUserName, HistoriaRPCPassword = @HistoriaRPCPassword";
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("IPFSHost", IPFSHost);
                        cmd.Parameters.AddWithValue("IPFSPort", IPFSPort);
                        cmd.Parameters.AddWithValue("IPFSApiHost", IPFSApiHost);
                        cmd.Parameters.AddWithValue("IPFSApiPort", IPFSApiPort);
                        cmd.Parameters.AddWithValue("HistoriaClientIPAddress", HistoriaClientIPAddress);
                        cmd.Parameters.AddWithValue("HistoriaRPCPort", HistoriaRPCPort);
                        cmd.Parameters.AddWithValue("HistoriaRPCUserName", HistoriaRPCUserName);
                        cmd.Parameters.AddWithValue("HistoriaRPCPassword", HistoriaRPCPassword);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED TO Update to SQLITE", ex.ToString());
            }
        }

        public static void LoadConfig()
        {
            try
            {
                CreateConfig();
                using (var connection = new SqliteConnection("Data Source=basex.db"))
                {
                    connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM basexConfiguration";
                        cmd.CommandType = System.Data.CommandType.Text;

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                IPFSHost = rdr.GetString(rdr.GetOrdinal("IPFSHost"));
                                IPFSPort = rdr.GetInt32(rdr.GetOrdinal("IPFSPort"));

                                IPFSApiHost = rdr.GetString(rdr.GetOrdinal("IPFSApiHost"));
                                IPFSApiPort = rdr.GetInt32(rdr.GetOrdinal("IPFSApiPort"));

                                HistoriaClientIPAddress = rdr.GetString(rdr.GetOrdinal("HistoriaClientIPAddress"));
                                HistoriaRPCPort = rdr.GetInt32(rdr.GetOrdinal("HistoriaRPCPort"));
                                HistoriaRPCUserName = rdr.GetString(rdr.GetOrdinal("HistoriaRPCUserName"));
                                HistoriaRPCPassword = rdr.GetString(rdr.GetOrdinal("HistoriaRPCPassword"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED TO Connect to SQLITE", ex.ToString());
            }
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            //I changed something
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddControllers(options => options.EnableEndpointRouting = false);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("HistoriaContextConnection")));

            services.AddMvc(option => option.EnableEndpointRouting = false);

            services.AddRazorPages();
            services.AddMvc().AddNewtonsoftJson();

            ApplicationSettings.LoadConfig();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                //app.UseExceptionHandler("/Home/Error");
                app.UseDeveloperExceptionPage();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!env.IsDevelopment())
                app.UseHttpsRedirection();


            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseCors();
            //app.UseAuthentication();
            //app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
 
        }
    }
}
