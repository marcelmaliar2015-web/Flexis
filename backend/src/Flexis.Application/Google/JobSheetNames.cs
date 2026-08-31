using System.Globalization;

namespace Flexis.Application.Google;

public static class JobSheetNames
{
    public static bool IsArchiveTab(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number >= 1
            && name == number.ToString(CultureInfo.InvariantCulture);
    }

    public static string NextArchiveTab(IEnumerable<string> existingNames)
    {
        var max = 0;
        foreach (var name in existingNames)
        {
            if (!IsArchiveTab(name)
                || !int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            if (number > max)
            {
                max = number;
            }
        }

        return (max + 1).ToString(CultureInfo.InvariantCulture);
    }
}
