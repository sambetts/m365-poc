using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ExcelProcessMessage
    {
        public string Name 
        { 
            get 
            { 
                return this.GetType().Name; 
            } 
        }
    }
    public class IncreaseMemoryRequest : ExcelProcessMessage
    {
        public int HowMuchInMegabytes { get; set; }
    }

    public class IncreaseMemoryResponse : ExcelProcessMessage
    {
        public int ProcessPrivateMemorySizeMB { get; set; }
    }
}
