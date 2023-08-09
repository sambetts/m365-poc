using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServerApp
{
    public class MemoryEater
    {

        public MemoryEater()
        {
            this.ByteDumps = new List<byte[]>();
        }

        
        public void IncreaseMbUsage(int mbIncrease)
        {

            int dumpTotalBytes = mbIncrease * Constants.ONE_THOUSAND_TWENTY_FOUR * Constants.ONE_THOUSAND_TWENTY_FOUR;

            byte[] buffer = new byte[dumpTotalBytes];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0x20;
            }

            ByteDumps.Add(buffer);
        }

        public List<byte[]> ByteDumps { get; set; }
    }
}
