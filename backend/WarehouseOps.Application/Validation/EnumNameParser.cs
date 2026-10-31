namespace WarehouseOps.Application.Validation;

public static class EnumNameParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var name = value.Trim();

        return Enum.TryParse(name, ignoreCase: true, out result)
            && Enum.IsDefined(result)
            && string.Equals(Enum.GetName(result), name, StringComparison.OrdinalIgnoreCase);
    }
}
