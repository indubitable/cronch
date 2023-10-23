using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace cronch.TagHelpers;

[HtmlTargetElement("label", Attributes = "bs5-for")]
public class Bootstrap5LabelTagHelper : LabelTagHelper
{
    public Bootstrap5LabelTagHelper(IHtmlGenerator generator) : base(generator)
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

        if (For.Metadata.IsRequired)
        {
            var span = new TagBuilder("span");
            span.AddCssClass("text-danger");
            span.AddCssClass("required");
            span.InnerHtml.Append(" *");
            output.Content.AppendHtml(span);
        }
    }
}
