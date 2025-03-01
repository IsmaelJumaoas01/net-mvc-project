using Microsoft.AspNetCore.Http;

namespace homeowner_app
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // adjust as needed
            });

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseSession(); // ✅ Enable Session Middleware (before authentication)
            app.UseAuthentication();
            app.UseAuthorization();

            // Middleware to check login status
            app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Allow access to login, register, static files, and home page
    if (path == "/" || path.StartsWith("/login") || path.StartsWith("/register") || path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images"))
    {
        await next();
        return;
    }

    // Check if session contains UserID
    if (context.Session.GetString("UserID") == null)
    {
        // Store the last attempted route
        context.Session.SetString("LastAttemptedRoute", path);
        context.Response.Redirect("/");
        return;
    }

    await next();
});


            // Configure endpoints
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Add route for Registration Page
            app.MapControllerRoute(
                name: "Register",
                pattern: "register",
                defaults: new { controller = "Account", action = "Register" }
            );

            // Add route for Profile Page
            app.MapControllerRoute(
                name: "Profile",
                pattern: "profile",
                defaults: new { controller = "Home", action = "Profile" }
            );

            app.Run();
        }
    }
}
