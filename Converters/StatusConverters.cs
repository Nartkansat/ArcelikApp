using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ArcelikExcelApp.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string role = value?.ToString() ?? "User";
            // Admin: Indigo Blue, User: Emerald Green (More premium colors)
            return role == "Admin" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3949AB")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#43A047"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ActiveToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)(value ?? false);
            // Active: Teal, Inactive: Rose/Red
            return isActive ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00897B")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ActiveToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)(value ?? false);
            return isActive ? "Aktif" : "Pasif";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = (bool)(value ?? false);
            return isTrue ? "CheckDecagram" : "AlertCircleOutline";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = (bool)(value ?? false);
            return isTrue ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7E84A3"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = (bool)(value ?? false);
            return isTrue ? "Aktif" : "Bekliyor";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToReadBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isRead = (bool)(value ?? false);
            return isRead ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F4FF"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NotificationTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value?.ToString() ?? "Info";
            bool isLight = parameter?.ToString() == "Light";

            string colorStr = type switch
            {
                "Success" => isLight ? "#E8F5E9" : "#4CAF50",
                "Warning" => isLight ? "#FFF3E0" : "#FF9800",
                "Error" => isLight ? "#FFEBEE" : "#F44336",
                _ => isLight ? "#E3F2FD" : "#2196F3"
            };
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NotificationTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value?.ToString() ?? "Info";
            return type switch
            {
                "Success" => "CheckCircle",
                "Warning" => "Alert",
                "Error" => "AlertOctagon",
                _ => "Information"
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
