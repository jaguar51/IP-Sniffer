using System;
using System.Collections.Generic;

namespace Http
{
    public class IPItem : IComparer<IPItem>
    {
        public String IP { get; set; }
        public int Count { get; set; }

        public IPItem(String ip, int count)
        {
            this.IP = ip;
            this.Count = count;
        }

        public int Compare(IPItem x, IPItem y)
        {
            return x.Count.CompareTo(y.Count);
        }
    }
}
