using Assignment.Middleware;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Assignment.TagHelpers;

/// <summary>Adds the request's Content-Security-Policy nonce to every &lt;script&gt; tag.</summary>
[HtmlTargetElement("script")]
public class CspNonceTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        string nonce = SecurityHeadersMiddleware.GetNonce(ViewContext.HttpContext);
        if (!string.IsNullOrEmpty(nonce))
            output.Attributes.SetAttribute("nonce", nonce);
    }
}
