using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServerApp
{
    class Server
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Excel out-of-process daemon running. Waiting for connection.");

            while (true)
            {
                using (var server = new NamedPipeServerStream(Constants.PIPE_NAME))
                {
                    server.WaitForConnection();
                    Console.WriteLine("New client connection.");
                    StreamReader reader = new StreamReader(server);
                    StreamWriter writer = new StreamWriter(server);

                    var clientMessage = reader.ReadLine();

                    dynamic data = JsonConvert.DeserializeObject(clientMessage);

                    // Do we have a valid message?
                    if (PropertyExist(data, "Name") && data.Name != "")
                    {
                        // Work out type
                        string typeName = data.Name;
                        Type msgType = Type.GetType(typeName);

                        ExcelProcessMessage response = null;

                        // Do something
                        if (typeName == nameof(IncreaseMemoryRequest))
                        {
                            var message = JsonConvert.DeserializeObject<IncreaseMemoryRequest>(clientMessage);
                            response = IncreaseMemory(message);
                        }

                        // Respond back
                        writer.WriteLine(JsonConvert.SerializeObject(response));
                        writer.Flush();
                        server.Disconnect();
                    }


                    Console.WriteLine("Disconnected from client.");
                }
            }
        }

        static Lazy<MemoryEater> memoryEater = new Lazy<MemoryEater>();
        private static IncreaseMemoryResponse IncreaseMemory(IncreaseMemoryRequest message)
        {
            // Increase memory
            memoryEater.Value.IncreaseMbUsage(message.HowMuchInMegabytes);

            // Return current usage
            System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();

            long privateMemoryMb = proc.PrivateMemorySize64 / Constants.ONE_THOUSAND_TWENTY_FOUR / Constants.ONE_THOUSAND_TWENTY_FOUR;

            return new IncreaseMemoryResponse() { ProcessPrivateMemorySizeMB = Convert.ToInt32(privateMemoryMb) };
        }

        static bool PropertyExist(dynamic settings, string name)
        {
            if (settings is System.Dynamic.ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings[name] != null;
        }
    }
}
