using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsCli
{
    class Item
    {
        private string _value = string.Empty;

        public string Name { get; set; }
        public string Value {
            get
            {
                return _value;
            }
            set
            {
                _value = (value == null ? string.Empty : value);
            }
        }
    }
}
