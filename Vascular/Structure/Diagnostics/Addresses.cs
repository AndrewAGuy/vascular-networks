using System;
using System.Collections.Generic;
using Vascular.Structure.Nodes;

namespace Vascular.Structure.Diagnostics
{
    /// <summary>
    /// 
    /// </summary>
    public static class Address
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        public static List<int> GetAddress(this Branch current, Branch relativeTo = null)
        {
            var addr = new List<int>();
            while (current.Start is not Source
                && current != relativeTo)
            {
                var s = current.Start;
                var i = Array.IndexOf(s.Downstream, current);
                addr.Add(i);
                current = s.Upstream;
            }
            addr.Reverse();
            return addr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Branch Navigate(Branch root, List<int> address)
        {
            for (var i = 0; i < address.Count; i++)
            {
                root = root.Children[address[i]];
            }
            return root;
        }
    }
}
