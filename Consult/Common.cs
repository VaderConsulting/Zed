using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Consult
{
    public static class GlobalMembers
    {
        internal static int[] TABLE_MASK = { 0x00000000, 0x00000001, 0x00000002, 0x00000004, 0x00000008, 0x00000010, 0x00000020, 0x00000040, 0x00000080 };
        internal static int[] TABLE_SHIFT = { 0x00, 0, 1, 2, 3, 4, 5, 6, 7 };
    }

    public class COMMSresponse
    {
        public string data = "";
        public int len;
    }
}
