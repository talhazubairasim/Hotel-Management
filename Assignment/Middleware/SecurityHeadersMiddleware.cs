using System.Security.Cryptography;

namespace Assignment.Middleware;

/// <summary>
/// Adds the HTTP security headers recommended by the OWASP Secure Headers Project
/// to every response, including a Content-Security-Policy with a per-request nonce
/// for inline scripts (see <see cref="TagHelpers.CspNonceTagHelper"/>).
/// </summary>
public class SecurityHeadersMiddleware
{
    public const string NonceItemKey = "csp-nonce";

    // Third-party hosts the layout loads icons, notifications and dialogs from.
    private const string Cdns = "https://cdnjs.cloudflare.com https://cdn.jsdelivr.net";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public static string GetNonce(HttpContext context) =>
        context.Items[NonceItemKey] as string ?? string.Empty;

    public Task InvokeAsync(HttpContext context)
    {
        string nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceItemKey] = nonce;

        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            h["Cross-Origin-Opener-Policy"] = "same-origin";
            h["Cross-Origin-Resource-Policy"] = "same-origin";
            h["X-Permitted-Cross-Domain-Policies"] = "none";
            h["Content-Security-Policy"] =
                "default-src 'self'; " +
                $"script-src 'self' 'nonce-{nonce}' {Cdns}; " +
                $"style-src 'self' 'unsafe-inline' {Cdns}; " +
                $"font-src 'self' {Cdns}; " +
                "img-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'";
            h.Remove("Server");
            h.Remove("X-Powered-By");
            return Task.CompletedTask;
        });

        return _next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
