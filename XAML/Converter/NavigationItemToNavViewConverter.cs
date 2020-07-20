using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WidgetLayout.Dto.Page.MainPage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace WidgetLayout.XAML.Converter
{
    public class NavigationItemToNavViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is NavigationViewSelectionChangedEventArgs args)
            {
                return args.SelectedItem;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}
