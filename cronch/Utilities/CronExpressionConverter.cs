namespace cronch.Utilities;

/// <summary>
/// Converts cron expressions from Cronos format to Quartz.NET format.
/// 
/// Key differences:
/// - Quartz day-of-week: 1=SUN..7=SAT; Cronos: 0=SUN..6=SAT (also accepts 7=SUN)
/// - Quartz requires exactly one of day-of-month or day-of-week to be '?' when the other is specified
/// - Both use 6 or 7 fields: sec min hr dom mon dow [year]
/// </summary>
public static class CronExpressionConverter
{
    /// <summary>
    /// Attempts to convert a Cronos-format cron expression to Quartz.NET format.
    /// Returns null if conversion fails.
    /// </summary>
    public static string? TryConvertCronosToQuartz(string? cronosExpression)
    {
        if (string.IsNullOrWhiteSpace(cronosExpression))
            return cronosExpression;

        try
        {
            return ConvertCronosToQuartz(cronosExpression);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a Cronos-format cron expression to Quartz.NET format.
    /// Throws <see cref="FormatException"/> if conversion fails.
    /// </summary>
    public static string ConvertCronosToQuartz(string cronosExpression)
    {
        var parts = cronosExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Cronos with seconds: sec min hr dom mon dow (6 fields)
        // Cronos does not support a year field
        if (parts.Length != 6)
            throw new FormatException($"Expected 6 fields in Cronos cron expression, got {parts.Length}: '{cronosExpression}'");

        var sec = parts[0];
        var min = parts[1];
        var hr = parts[2];
        var dom = parts[3];
        var mon = parts[4];
        var dow = parts[5];

        // Convert day-of-week values: Cronos 0-6 (or 7) → Quartz 1-7
        dow = ConvertDayOfWeekField(dow);

        // Apply the '?' wildcard rule:
        // If both dom and dow are wildcards (* or ?), set dow to '?'
        // If dom is specific and dow is wildcard, set dow to '?'
        // If dow is specific and dom is wildcard, set dom to '?'
        // If both are specific, set dom to '?' (dow takes precedence, matching Cronos behavior)
        (dom, dow) = ApplyQuestionMarkRule(dom, dow);

        return $"{sec} {min} {hr} {dom} {mon} {dow}";
    }

    private static string ConvertDayOfWeekField(string field)
    {
        // Handle composite expressions (e.g., "1-5", "0,3,6", "1/2", "1-5/2")
        // We need to increment all numeric day-of-week values by 1
        // But named days (SUN, MON, etc.) are already correct in Quartz

        if (field == "*" || field == "?")
            return field;

        // Process each comma-separated part
        var parts = field.Split(',');
        var converted = new string[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            converted[i] = ConvertDayOfWeekPart(parts[i]);
        }

        return string.Join(',', converted);
    }

    private static string ConvertDayOfWeekPart(string part)
    {
        // Handle step: e.g., "1-5/2" or "*/2" or "0/1"
        var stepSplit = part.Split('/');
        if (stepSplit.Length == 2)
        {
            var basePart = ConvertDayOfWeekRangeOrValue(stepSplit[0]);
            // Step value is not a day-of-week value, keep as-is
            return $"{basePart}/{stepSplit[1]}";
        }

        return ConvertDayOfWeekRangeOrValue(part);
    }

    private static string ConvertDayOfWeekRangeOrValue(string part)
    {
        // Handle range: e.g., "1-5"
        var rangeSplit = part.Split('-');
        if (rangeSplit.Length == 2)
        {
            return $"{IncrementDowValue(rangeSplit[0])}-{IncrementDowValue(rangeSplit[1])}";
        }

        // Handle 'L' suffix (Quartz-specific, but handle gracefully)
        if (part.EndsWith('L'))
        {
            return $"{IncrementDowValue(part[..^1])}L";
        }

        // Handle '#' (nth weekday, e.g., "5#3" = third Friday)
        var hashSplit = part.Split('#');
        if (hashSplit.Length == 2)
        {
            return $"{IncrementDowValue(hashSplit[0])}#{hashSplit[1]}";
        }

        return IncrementDowValue(part);
    }

    private static string IncrementDowValue(string value)
    {
        if (value == "*" || value == "?")
            return value;

        // Named days don't need conversion
        if (!int.TryParse(value, out var numericValue))
            return value;

        // Cronos: 0=SUN..6=SAT, 7=SUN
        // Quartz: 1=SUN..7=SAT
        if (numericValue == 7)
            return "1"; // 7 (SUN in Cronos) → 1 (SUN in Quartz)

        if (numericValue < 0 || numericValue > 6)
            throw new FormatException($"Invalid day-of-week value: {value}");

        return (numericValue + 1).ToString();
    }

    private static bool IsWildcard(string field)
    {
        return field == "*" || field == "?";
    }

    private static (string dom, string dow) ApplyQuestionMarkRule(string dom, string dow)
    {
        bool domIsWild = IsWildcard(dom);
        bool dowIsWild = IsWildcard(dow);

        if (domIsWild && dowIsWild)
        {
            // Both wildcards: keep dom as *, set dow to ?
            return (dom == "?" ? "*" : dom, "?");
        }
        else if (!domIsWild && dowIsWild)
        {
            // dom is specific, dow is wildcard → dow becomes ?
            return (dom, "?");
        }
        else if (domIsWild && !dowIsWild)
        {
            // dow is specific, dom is wildcard → dom becomes ?
            return ("?", dow);
        }
        else
        {
            // Both specific: Cronos allows this, Quartz doesn't.
            // Use dow, set dom to '?' (day-of-week takes precedence)
            return ("?", dow);
        }
    }
}
