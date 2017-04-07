using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoEventSource
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var eslistener = new ESEventListener())
            {
                using (var eventListener = new EventSourceSamples.ConsoleEventListener())
                {
                    EventSourceSamples.CustomizedEventSourceDemo.Run();
                }
            }
            //EventSourceSamples.AllSamples.Run();
            Console.WriteLine("Completed, press any key");
            Console.ReadKey();
        }
    }
}
