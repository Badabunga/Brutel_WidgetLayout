using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WidgetLayout.Interfaces.ViewModel.Base
{
    public interface IBaseViewModel : INotifyPropertyChanged
    {
        string Title { get; set; }
    }
}
