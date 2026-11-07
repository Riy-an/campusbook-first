using System.Globalization;

namespace CampusBook.Framework;

public static class Extensions
{
    public static string ToDateText(this DateTime? date)
    {
        return date.HasValue ? date.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : "-";
    }

    public static string ToDateText(this DateTime date)
    {
        return date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    public static string ToDateTimeText(this DateTime date)
    {
        return date.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
    }

    public static string ToPriceText(this decimal price)
    {
        return price <= 0 ? "Free / Donation" : (price % 1 == 0 ? $"৳{price:N0}" : $"৳{price:N2}");
    }

    public static string Truncate(this string? value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..(maxLength - suffix.Length)] + suffix;
    }
}

