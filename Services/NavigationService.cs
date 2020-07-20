using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WidgetLayout.Interfaces.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WidgetLayout.Services
{
    public class NavigationService : INavigationService
    {
        private Dictionary<string, Type> _pageDict;

        private Frame CurrentWindowFrame { get => MainPage.MainPageFrame; }

        public NavigationService()
        {
            this._pageDict = new Dictionary<string, Type>();
        }

        public bool NavigateTo(string pageName, object param = null)
        {
            var retValue = false;

            if(!string.IsNullOrEmpty(pageName) && this._pageDict.TryGetValue(pageName, out var pageType))
            {
                if(this.CurrentWindowFrame != null)
                {
                    retValue = this.CurrentWindowFrame.NavigateToType(pageType, param, new Windows.UI.Xaml.Navigation.FrameNavigationOptions { IsNavigationStackEnabled = true });
                }
            }

            return retValue;
        }


        public void RegisterPage(string pageName, Type pageType)
        {
            if(!this._pageDict.ContainsKey(pageName))
            {
                this._pageDict[pageName] = pageType;
            }
        }
    }
}
