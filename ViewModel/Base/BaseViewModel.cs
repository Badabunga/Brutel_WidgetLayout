using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WidgetLayout.Interfaces.ViewModel.Base;

namespace WidgetLayout.ViewModel.Base
{
    public abstract class BaseViewModel : IBaseViewModel
    {
        private string _title;

        public BaseViewModel()
        {

        }

      

        public string Title { get => this._title; set => this.SetProperty(ref this._title, value); }


        protected void SetProperty<T>(ref T backValue, T value, [CallerMemberName] string propertyName = "")
        {
            var comparer = EqualityComparer<T>.Default;

            if(!comparer.Equals(backValue,value))
            {
                backValue = value;
                this.NotifyPropertyChanged(propertyName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
