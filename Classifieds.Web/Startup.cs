using Classifieds.Data;
using Classifieds.Data.Entities;
using Classifieds.Web.Constants;
using Classifieds.Web.Services;
using Classifieds.Web.Services.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Classifieds.Web
{
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DatabaseConnection"))
            );

            services.AddTransient<IEmailSender>(s => new EmailSender("localhost", 25, "no-reply@classified.com"));

            services.AddDefaultIdentity<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.SignIn.RequireConfirmedAccount = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 3;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddPasswordValidator<PasswordValidatorService>()
                .AddClaimsPrincipalFactory<CustomClaimsService>();

            services.AddAuthentication()
                .AddGoogle(googleOptions => {
                    googleOptions.ClientId = Configuration["Google:ClientId"];
                    googleOptions.ClientSecret = Configuration["Google:ClientSecret"];
                });
              

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

                options.AddPolicy(Policies.IsMinimumAge, policy =>
                    policy.RequireClaim(UserClaims.isMinimumAge, "true"));
            });

            services.AddRazorPages().AddMvcOptions(q => q.Filters.Add(new AuthorizeFilter()));

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                //o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
                .AddCookie(q => q.LoginPath = "/Auth/Login")
                .AddGoogle(o => { o.ClientId = Configuration["Google:ClientId"]; o.ClientSecret = Configuration["Google:ClienteSecret"]; })
                //.AddTwitter(o => { o.ConsumerKey = ""; o.ConsumerSecret = ""; })
                //.AddFacebook(o => { o.ClientId = ""; o.ClientSecret = ""; })
                //.AddMicrosoftAccount(o => { o.ClientId = ""; o.ClientSecret = ""; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
