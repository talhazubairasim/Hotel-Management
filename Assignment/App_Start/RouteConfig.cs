namespace Assignment.App_Start;

/// <summary>Route configuration (kept in App_Start, as in classic ASP.NET MVC).</summary>
public static class RouteConfig
{
    public static void RegisterRoutes(IEndpointRouteBuilder routes)
    {
        routes.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    }
}
