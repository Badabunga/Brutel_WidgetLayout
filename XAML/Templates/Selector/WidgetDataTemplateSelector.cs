using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WidgetLayout.Dto.Page.WidgetPage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WidgetLayout.XAML.Templates.Selector
{
    public class WidgetDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate WorkItem { get; set; }

        public DataTemplate SourcePackage { get; set; }

        public DataTemplate DestinationPackage { get; set; }

        public DataTemplate Documents { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if(item is WorkItemDto)
            {
                return this.WorkItem;
            }

            else if(item is PackageDto packageDto)
            {
                if(packageDto.IsSourcePackage)
                {
                    return this.SourcePackage;
                }
                else
                {
                    return this.DestinationPackage;
                }
            }

            else if(item is DocumentContainer)
            {
                return this.Documents;
            }
            else
            {
                return null;
            }
        }
    }
}
