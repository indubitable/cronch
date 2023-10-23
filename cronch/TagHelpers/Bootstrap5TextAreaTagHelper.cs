using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace cronch.TagHelpers;

[HtmlTargetElement("textarea", Attributes = "bs5-for")]
public class Bootstrap5TextAreaTagHelper : TextAreaTagHelper
{

    public Bootstrap5TextAreaTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }

    [HtmlAttributeName("bs5-for")]
    public ModelExpression Bs5For
    {
        get => For;
        set => For = value;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);

        if (ViewContext.ViewData.ModelState.Count > 0 && ViewContext.ViewData.ModelState.ErrorCount > 0)
        {
            var classes = output.GetClasses();

            if (classes.Contains("input-validation-error"))
            {
                output.AddClass("is-invalid", HtmlEncoder.Default);
            }
            else
            {
                output.AddClass("is-valid", HtmlEncoder.Default);
            }
        }
    }
}
