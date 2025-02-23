namespace homeowner_app
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddSession(); // ✅ Add Session Services

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession(); // ✅ Enable Session Middleware

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