using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Assignment.App_Start;

/// <summary>Global MVC filters (kept in App_Start, as in classic ASP.NET MVC).</summary>
public static class FilterConfig
{
    public static void RegisterGlobalFilters(MvcOptions options)
    {
        // Every page requires a signed-in staff member unless marked [AllowAnonymous].
        options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        // Every POST must carry a valid anti-forgery token.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    }
}
