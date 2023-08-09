using Common;
using ExcelDna.Integration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFunctions
{
    public static class ExcelFunctions
    {
        [ExcelFunction(Description = "My first .NET function")]
        public static string SayHello(string name)
        {
            return "Hello " + name;
        }

        [ExcelFunction(Description = "Test Out")]
        public static long IncreaseMemory(int mb)
        {
            // Client
            using (var client = new NamedPipeClientStream(Constants.PIPE_NAME))
            {
                client.Connect();
                StreamReader reader = new StreamReader(client);
                StreamWriter writer = new StreamWriter(client);

                // Create request
                IncreaseMemoryRequest req = new IncreaseMemoryRequest() { HowMuchInMegabytes = mb };

                // Send over pipe to out-of-process daemon
                writer.WriteLine(JsonConvert.SerializeObject(req));
                writer.Flush();

                // Get response
                string responseMsg = reader.ReadLine();
                IncreaseMemoryResponse response = JsonConvert.DeserializeObject<IncreaseMemoryResponse>(responseMsg);

                return response.ProcessPrivateMemorySizeMB;
            }
        }
    }
}
