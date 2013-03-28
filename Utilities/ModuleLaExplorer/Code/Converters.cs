using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace LaExplorer.Code
{
    public class DateFormatConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter != null)
            {
                return string.Format(parameter.ToString(), value);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(DateTime) || targetType == typeof(Nullable<DateTime>))
            {
                DateTime d;
                if (DateTime.TryParse(value.ToString(), out d))
                {
                    return d;
                }
                else
                {
                    return DateTime.Now;
                }

            }
            else if (targetType == typeof(Decimal) || targetType == typeof(Nullable<Decimal>))
            {
                decimal n = 0;
                decimal.TryParse(value.ToString(), out n);
                return n;

            }
            else if (targetType == typeof(Int32) || targetType == typeof(Nullable<Int32>))
            {
                int n = 0;
                int.TryParse(value.ToString(), out n);
                return n;
            }

            return value;
        }

        #endregion
    }

    public class FileSizeFormatConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value[0] != null && (long)value[0] != 0)
            {
                string[] letters = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };

                byte i = 0;
                decimal size = System.Convert.ToDecimal(value[0]);
                while (size / 1024 >= 1)
                {
                    i++;
                    size /= 1024;
                }
                decimal precision = System.Convert.ToDecimal(value[0]) / (i == 0 ? 1 : (decimal)System.Math.Exp(i * System.Math.Log(1024)));
                return String.Format("{0:0.00} {1}", precision, letters[i]);

            }
            return value;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class CenterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value / 4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class InvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return -(double)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public enum PageTransitionType
    {
        Fade,
        Slide,
        SlideAndFade,
        Grow,
        GrowAndFade,
        Flip,
        FlipAndFade,
        Spin,
        SpinAndFade
    }
}
