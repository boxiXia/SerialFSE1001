using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Timers;
namespace SerialCommunicationCsharp
{
    class Test
    {
        static SerialThreadFSE1001 sensor;
        static void Main(string[] args)
        {
            sensor = new SerialThreadFSE1001();
            sensor.StartThread();
            sensor.InitializeSensor();

            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 3000;
            aTimer.Enabled = true;
            aTimer.AutoReset = false;
            while (sensor.IsLooping())
            {
                if (sensor.receiveQueue.Count != 0)
                {
                    FSE1001 sample = (FSE1001)sensor.receiveQueue.Dequeue();
                    sample.Print();
                }
            }
            while (Console.Read() != 'q') { }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            // refer to: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        // Specify what you want to happen when the Elapsed event is raised.
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            sensor.StopThread();
            Console.WriteLine("Press \'q\' to quit the sample.");
        }
    }







}