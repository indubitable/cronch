using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace cronch.TagHelpers;

public static class TagHelperExtensions
{
    private static readonly char[] SpaceChars = { '\u0020', '\u0009', '\u000A', '\u000C', '\u000D' };

    public static List<string> GetClasses(this TagHelperOutput tagHelperOutput)
    {
        var htmlEncoder = HtmlEncoder.Default;
        if (!tagHelperOutput.Attributes.TryGetAttribute("class", out TagHelperAttribute classAttribute))
        {
            return new List<string>();
        }
        else
        {
            var currentClassValue = ExtractClassValue(classAttribute, htmlEncoder);
            var encodedSpaceChars = SpaceChars.Where(x => !x.Equals('\u0020')).Select(x => htmlEncoder.Encode(x.ToString())).ToArray();
            return currentClassValue.Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(perhapsEncoded => perhapsEncoded.Split(encodedSpaceChars, StringSplitOptions.RemoveEmptyEntries))
                .ToList();
        }
    }

    private static string ExtractClassValue(TagHelperAttribute classAttribute, HtmlEncoder htmlEncoder)
    {
        string? extractedClassValue;
        switch (classAttribute.Value)
        {
            case string valueAsString:
                extractedClassValue = htmlEncoder.Encode(valueAsString);
                break;
            case HtmlString valueAsHtmlString:
                extractedClassValue = valueAsHtmlString.Value;
                break;
            case IHtmlContent htmlContent:
                using (var stringWriter = new StringWriter())
                {
                    htmlContent.WriteTo(stringWriter, htmlEncoder);
                    extractedClassValue = stringWriter.ToString();
                }
                break;
            default:
                extractedClassValue = htmlEncoder.Encode(classAttribute.Value?.ToString() ?? string.Empty);
                break;
        }
        var currentClassValue = extractedClassValue ?? string.Empty;
        return currentClassValue;
    }
}
