using System.Globalization;

namespace MoneyMindApp.Converters;

/// <summary>
/// Converter to invert boolean value
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

/// <summary>
/// Converter for showing/hiding values (returns **** if not visible)
/// </summary>
public class VisibilityValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2)
            return "****";

        var value = values[0]?.ToString() ?? "****";
        var isVisible = values[1] is bool visible && visible;

        return isVisible ? value : "****";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for boolean to eye icon (👁 or 👁‍🗨)
/// </summary>
public class BoolToEyeIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
        {
            return isVisible ? "👁" : "👁‍🗨";
        }
        return "👁";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for income boolean to icon (📈 or 📉)
/// </summary>
public class IncomeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncome)
        {
            return isIncome ? "📈" : "📉";
        }
        return "📊";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for income boolean to color (green or red)
/// </summary>
public class IncomeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isIncome)
        {
            return isIncome ? Colors.Green : Colors.Red;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for checking if object is not null
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for hex color string to Color
/// Converts #RRGGBB string to Color object, with fallback to parameter color
/// </summary>
public class ColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            try
            {
                return Color.FromArgb(hexColor);
            }
            catch
            {
                // If parsing fails, return parameter or default
            }
        }

        // Return fallback color from parameter or default primary
        if (parameter is Color fallbackColor)
            return fallbackColor;

        return Colors.Purple; // Default primary color
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to check if an account is selected (compares IDs)
/// Returns true if value equals parameter
/// </summary>
public class SelectedAccountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.Equals(parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for boolean to color based on parameter string
/// Parameter format: "TrueColor|FalseColor" (hex or color names)
/// Example: "#4CAF50|#F44336" or "Green|Red"
/// Default (no parameter): Green for true, Red for false
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    // Default colors used when no parameter is provided
    private static readonly Color DefaultTrueColor = Color.FromArgb("#10B981"); // Success green
    private static readonly Color DefaultFalseColor = Color.FromArgb("#EF4444"); // Danger red

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return Colors.Gray;

        // No parameter: use default green/red
        if (parameter is not string colorPair || string.IsNullOrEmpty(colorPair))
            return boolValue ? DefaultTrueColor : DefaultFalseColor;

        var colors = colorPair.Split('|');
        if (colors.Length != 2)
            return boolValue ? DefaultTrueColor : DefaultFalseColor;

        try
        {
            var selectedColor = boolValue ? colors[0] : colors[1];

            // Check if it's transparent
            if (selectedColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
                return Colors.Transparent;

            // Try to parse as hex
            if (selectedColor.StartsWith("#"))
                return Color.FromArgb(selectedColor);

            // Try to parse as named color
            return Color.FromArgb(selectedColor);
        }
        catch
        {
            return Colors.Gray;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for boolean to string based on parameter
/// Parameter format: "TrueText|FalseText"
/// Example: "Arresta Server|Avvia Server"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return string.Empty;

        if (parameter is not string textPair || string.IsNullOrEmpty(textPair))
            return boolValue.ToString();

        var texts = textPair.Split('|');
        if (texts.Length != 2)
            return boolValue.ToString();

        return boolValue ? texts[0] : texts[1];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for checking if string is not null or empty
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Alias for InverseBoolConverter for easier naming
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

/// <summary>
/// Converter to check if a transaction is selected
/// Accepts: [0] Transaction, [1] SelectedTransactions collection
/// Returns: true if transaction is in the selected list
/// </summary>
public class TransactionSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2)
            return false;

        var transaction = values[0] as Models.Transaction;
        var selectedTransactions = values[1] as System.Collections.ObjectModel.ObservableCollection<Models.Transaction>;

        if (transaction == null || selectedTransactions == null)
            return false;

        return selectedTransactions.Contains(transaction);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for transaction background color based on selection state
/// Accepts: [0] Transaction, [1] SelectedTransactions collection, [2] IsMultiSelectMode
/// Returns: Highlighted color if selected in multi-select mode, default otherwise
/// </summary>
public class TransactionSelectedBackgroundConverter : IMultiValueConverter
{
    private static readonly Color SelectedBackgroundLight = Color.FromArgb("#E8F5E9"); // Light green tint
    private static readonly Color SelectedBackgroundDark = Color.FromArgb("#1B5E20"); // Dark green tint
    private static readonly Color DefaultBackgroundLight = Color.FromArgb("#FFFBFE");
    private static readonly Color DefaultBackgroundDark = Color.FromArgb("#2B2930");

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        System.Diagnostics.Debug.WriteLine($"[CONVERTER] TransactionSelectedBackgroundConverter.Convert CALLED - values.Length={values.Length}");

        if (values.Length != 3)
        {
            System.Diagnostics.Debug.WriteLine($"[CONVERTER] Invalid values length, returning default");
            return DefaultBackgroundLight;
        }

        var transaction = values[0] as Models.Transaction;
        var selectedTransactions = values[1] as System.Collections.ObjectModel.ObservableCollection<Models.Transaction>;
        var isMultiSelectMode = values[2] is bool multiSelect && multiSelect;

        System.Diagnostics.Debug.WriteLine($"[CONVERTER] Transaction={transaction?.Descrizione ?? "NULL"}, IsMultiSelectMode={isMultiSelectMode}");

        if (transaction == null || !isMultiSelectMode)
        {
            System.Diagnostics.Debug.WriteLine($"[CONVERTER] Not in multi-select or null transaction, returning default");
            // Not in multi-select mode or invalid data - return theme-aware default
            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? DefaultBackgroundDark
                : DefaultBackgroundLight;
        }

        // ✅ Use transaction.IsSelected property instead of checking collection
        // This ensures the UI updates immediately when IsSelected changes
        bool isSelected = transaction.IsSelected;

        System.Diagnostics.Debug.WriteLine($"[CONVERTER] Transaction.IsSelected={isSelected}, returning {(isSelected ? "SELECTED" : "DEFAULT")} background");

        if (isSelected)
        {
            // Selected - return theme-aware highlight color
            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? SelectedBackgroundDark
                : SelectedBackgroundLight;
        }

        // Not selected - return theme-aware default
        return Application.Current?.RequestedTheme == AppTheme.Dark
            ? DefaultBackgroundDark
            : DefaultBackgroundLight;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for percentage value to color gradient
/// 0-50% = Red, 50-80% = Orange, 80-100% = Green
/// </summary>
public class PercentageToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double percentage)
            return Colors.Gray;

        // Gradient from red to green based on percentage
        if (percentage >= 80)
            return Color.FromArgb("#10B981"); // Green for 80-100%
        else if (percentage >= 50)
            return Color.FromArgb("#F59E0B"); // Orange for 50-80%
        else
            return Color.FromArgb("#EF4444"); // Red for 0-50%
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
