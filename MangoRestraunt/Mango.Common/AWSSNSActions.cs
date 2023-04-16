using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mango.Common
{
    public static class AWSSNSActions
    {
        public static string Action { get; set; } = "Action";
        public static string Checkout { get; set; } = "Checkout";
        public static string PaymentRequest { get; set; } = "PaymentRequest";
        public static string PaymentUpdate { get; set; } = "PaymentUpdate";
        public static string DataTypeString { get; set; } = "String";
    }
}
