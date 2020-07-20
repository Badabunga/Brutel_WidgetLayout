using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WidgetLayout.Dto.Page.WidgetPage
{
    public class PackageDto
    {
        public bool IsSourcePackage { get; set; }

        public double Capacity { get; set; }

        public double Amount { get; set; }

        public string PackageName { get; set; }
    }
}
