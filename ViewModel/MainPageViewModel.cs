using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WidgetLayout.Constants;
using WidgetLayout.Dto.Page.MainPage;
using WidgetLayout.Interfaces.Services;
using WidgetLayout.Interfaces.ViewModel;
using WidgetLayout.ViewModel.Base;
using WidgetLayout.XAML.Commands;

namespace WidgetLayout.ViewModel
{
    public class MainPageViewModel : BaseViewModel, IMainPageViewModel
    {
        private readonly INavigationService _navigationService;

        public ObservableCollection<NavViewItem> NavigationItems { get; set; }

        public ICommand NavItemClickedCommand { get; set; }

        public MainPageViewModel(INavigationService navigationService)
        {
            this._navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.NavItemClickedCommand = new Command<NavViewItem>(this.NavigateToItem);
            this.FillNaviationItems();
        }


        private void NavigateToItem(NavViewItem param)
        {
            if(param is NavViewItem navViewItem)
            {
                this._navigationService.NavigateTo(navViewItem.Tag);
            }

        }

        private void FillNaviationItems() => this.NavigationItems = new ObservableCollection<NavViewItem>
        {
            new NavViewItem{Name = "Widgets", Icon = GlyphConstants.WidgetPage, Tag = PageConstants.WidgetPage}
        };
    }
}
