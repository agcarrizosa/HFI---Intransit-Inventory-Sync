using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intransit_Inventory_Sync
{
    class Error
    {
        public string ErrorMessage { get; set; }
        public string ItemNUmber { get; set; }
        public string Location { get; set; }
        public string Quantity { get; set; }
    }
}
