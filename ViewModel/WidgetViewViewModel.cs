using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WidgetLayout.Dto.Page.WidgetPage;
using WidgetLayout.Interfaces.ViewModel;
using WidgetLayout.ViewModel.Base;
using WidgetLayout.XAML.Commands;

namespace WidgetLayout.ViewModel
{
    public class WidgetViewViewModel : BaseViewModel, IWidgetViewModel
    {
        public ICommand AddNewWorkItemCommand { get; set; }
        public ICommand AddDestPackagekItemCommand { get; set; }
        public ICommand AddSrcNewWorkItem { get; set; }
        public ICommand AddNewDockItem { get; set; }


        public System.Collections.ObjectModel.ObservableCollection<object> Widgets { get; set; }

        private Random _random;

        public WidgetViewViewModel()
        {
            this.Title = "Widgets";
            this.Widgets = new System.Collections.ObjectModel.ObservableCollection<object>();
            this.AddNewWorkItemCommand = new Command<object>(this.WorkItem);
            this.AddDestPackagekItemCommand = new Command<object>(this.DestPackage);
            this.AddSrcNewWorkItem = new Command<object>(this.SourcePackage);
            this.AddNewDockItem = new Command<object>(this.Document);
            this._random = new Random();
        }




        private void WorkItem(object parameter)
        {
            this.Widgets.Add(new WorkItemDto
            {
                Name = $"Arbeitsobjekt {this.Widgets.Count}",
                InfoText = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.", 
                State = "OK"
            });
        }
        private void SourcePackage (object parameter)
        {
            var capacity = this._random.Next(0,100);
            var amount = this._random.Next(0, capacity);

            this.Widgets.Add(new PackageDto
            {
                IsSourcePackage = true,
                Amount = amount,
                Capacity = capacity,
                PackageName = $"Packmittel : {this.Widgets.Count}"

            });
        }
        private void DestPackage(object parameter)
        {
            var capacity = this._random.Next(0, 100);
            var amount = this._random.Next(0, capacity);

            this.Widgets.Add(new PackageDto
            {
                IsSourcePackage = false,
                Amount = amount,
                Capacity = capacity,
                PackageName = $"Packmittel : {this.Widgets.Count}"

            });

        }

        private void Document(object parameter)
        {

        }
    }
}
