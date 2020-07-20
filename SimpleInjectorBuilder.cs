using AutoMapper;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WidgetLayout.Constants;
using WidgetLayout.Interfaces.Services;
using WidgetLayout.Interfaces.ViewModel;
using WidgetLayout.Services;
using WidgetLayout.View;
using WidgetLayout.ViewModel;

namespace WidgetLayout
{
    public static class SimpleInjectorBuilder
    {
        public static Container GetContainer()
         {
            var container = new Container();
            // Singleton
            var navigationService = RegisterPages();
            container.Register<INavigationService>(() => navigationService, Lifestyle.Singleton);
            //Services 


            // ViewModels
            container.Register<IMainPageViewModel, MainPageViewModel>();
            container.Register<IWidgetViewModel, WidgetViewViewModel>();

            return container;
        }


        private static INavigationService RegisterPages()
        {
            var navigationService = new NavigationService();


            navigationService.RegisterPage(PageConstants.WidgetPage, typeof(WidgetView));


            return navigationService;
        }

    }
}
