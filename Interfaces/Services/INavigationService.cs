using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WidgetLayout.Interfaces.Services
{
    public interface INavigationService
    {
        bool NavigateTo(string pageName, object parameter = null);

        void RegisterPage(string pageName, Type pageType);
    }
}
