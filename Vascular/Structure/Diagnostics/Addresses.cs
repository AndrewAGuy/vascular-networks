using System;
using System.Linq;
using System.Text;
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
        /// <param name="b"></param>
        /// <returns></returns>
        public static string GetAddress(this Branch b)
        {
            var s = new StringBuilder();
            while (b.Start is Bifurcation bf)
            {
                var i = bf.IndexOf(b);
                s.Append((char)(i + '0'));
                b = bf.Upstream;
            }
            return s.ToString().Reverse().ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Branch Navigate(Branch root, string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                root = root.Children[str[i] - '0'];
            }
            return root;
        }
    }
}
