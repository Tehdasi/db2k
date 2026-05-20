using System;
using System.Collections.Generic;
using System.Text;

namespace db2k
{
    internal class Misc
    {
        public static void AddBase64String(byte[] buffer, StreamWriter sw)
        {
            const int chunkSize = 2000;

            var b64 = Convert.ToBase64String(buffer);

            List<string> chunks = new();

            for (int ch = 0; ch < b64.Length; ch += chunkSize)
            {
                int cs = chunkSize;
                if (cs + ch > b64.Length)
                    cs = b64.Length - ch;

                chunks.Add(b64.Substring(ch, cs));
            }

            sw.WriteLine(string.Join(" +\r\n", chunks.Select(x => $"'{x}'")));
        }
    }
}
