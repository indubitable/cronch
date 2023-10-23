using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace cronch.TagHelpers;

[HtmlTargetElement("select", Attributes = "bs5-for")]
public class Bootstrap5SelectTagHelper : SelectTagHelper
{
    public Bootstrap5SelectTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }

    [HtmlAttributeName("bs5-for")]
    public ModelExpression Bs5For
    {
        get => For;
        set => For = value;
    }

    [HtmlAttributeName("bs5-items")]
    public IEnumerable<SelectListItem> Bs5Items
    {
        get => Items;
        set => Items = value;
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
