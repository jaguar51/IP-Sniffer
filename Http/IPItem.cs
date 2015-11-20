using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Http
{
    public class IPItem
    {
        public IPItem(String ip, int count)
        {
            this.IP = ip;
            this.Count = count;
        }
        public String IP { get; set; }
        public int Count { get; set; }
    }
}
