using Mango.Web.Services;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace Mango.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient<IProductService, ProductService>();
            builder.Services.AddHttpClient<ICartService, CartService>();
            builder.Services.AddHttpClient<ICouponService, CouponService>();
            SD.ProductAPIBase = builder.Configuration["ServiceUrls:ProductAPI"];
            SD.ShoppingCartAPIBase = builder.Configuration["ServiceUrls:ShoppingCartAPI"];
            SD.CouponAPIBase = builder.Configuration["ServiceUrls:CouponAPI"];
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICouponService, CouponService>();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireClaim("ClaimType", "Admin"));
                options.AddPolicy("AuthenticatedUsers", policy => policy.RequireClaim("ClaimType", "Admin", "Users"));
            });


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.MetadataAddress = builder.Configuration["Authentication:Cognito:MetadataAddress"];
                options.ClientId = builder.Configuration["Authentication:Cognito:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Cognito:ClientSecret"];
                options.ResponseType = builder.Configuration["Authentication:Cognito:ResponseType"];
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.Events = new OpenIdConnectEvents()
                {
                    OnTokenValidated = context =>
                    {
                        var group = context.Principal.Claims.Where(w => w.Type == "cognito:groups").FirstOrDefault()?.Value;
                        var claims = new List<Claim>();
                        var claimType = "ClaimType";
                        if (group == "Admin")
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "Users"));
                        }
                        var appIdentity = new ClaimsIdentity(claims);
                        context.Principal.AddIdentity(appIdentity);
                        return Task.CompletedTask;
                        
                    }
                };
            });
            //.AddJwtBearer(options =>
            //{

            //    options.Events = new JwtBearerEvents()
            //    {

            //        OnTokenValidated = context =>
            //        {
            //            // Check if the user has an OID claim
            //            //if (!context.Principal.HasClaim(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier"))
            //            //{
            //            //    context.Fail($"The claim 'oid' is not present in the token.");
            //            //}

            //            //ClaimsPrincipal userPrincipal = context.Principal;

            //            //// Check is user exists, if not then insert the user in our own database
            //            //CheckUser cu = new CheckUser(
            //            //    context.HttpContext.RequestServices.GetRequiredService<DBContext>(),
            //            //    context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>(),
            //            //    userPrincipal);

            //            //cu.CreateUser();
                        
            //        },
            //    };
            //});
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            
            app.UseAuthorization();

            

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            

            app.Run();
        }
    }
}