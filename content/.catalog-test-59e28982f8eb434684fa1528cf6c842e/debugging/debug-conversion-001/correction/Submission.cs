using System.Globalization;

public static class Submission
{
    public static int ParseQuantity(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) && quantity > 0
            ? quantity
            : 0;
}
