using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Collections;

namespace SerialCommunicationCsharp
{
        public class SerialThread
        {
            // please refer to https://www.alanzucconi.com/2016/12/01/asynchronous-serial-communication/
            // This is a class that enables asynchronous serial communication, based on Alan Zucconi's article
            public SerialPort serialPort;
            public byte[] buffer;
            public string message;
            public Queue transmitQueue;  // queue to send command
            public Queue receiveQueue;   // queue to receive info
            public Thread thread;
            public bool looping = true;
            public SerialThread(string portName = "COM3", int readBufferSize = 12, int baudRate = 19200)
            {
                serialPort = new SerialPort
                {
                    PortName = portName,
                    ReadBufferSize = readBufferSize,
                    BaudRate = baudRate,
                    DataBits = 8,
                    Handshake = Handshake.None,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                };
                // Creates the thread
                transmitQueue = Queue.Synchronized(new Queue());
                receiveQueue = Queue.Synchronized(new Queue());
                thread = new Thread(ThreadLoop);
            }
            public void StartThread()
            {
                thread.Start();
            }
            public virtual void ThreadLoop()
            {
                // runs the looping
                // step 1: Opens the connection on the serial port
                serialPort.Open();
                // step 2: initialization(optional)

                // step 3: start looping
                while (IsLooping())
                {
                    //do something here
                    // e.g ReadSerial()
                }
                // step 4: close the serial port
                serialPort.Close();
            }

            public bool IsLooping()
            {
                lock (this)
                {
                    return looping;
                }
            }
            public void StopThread()
            {
                lock (this)
                {
                    looping = false;
                }
            }
            public virtual void ReadSerial()
            {

            }
            public void WriteToSerial<T>(T command)
            {
                if (command is byte[])
                {
                    // write byte[]
                    byte[] commandT = (byte[])(object)command;
                    serialPort.Write(commandT, 0, commandT.Length);
                }
                else if (command is char[])
                {
                    //write char[]
                    char[] commandT = (char[])(object)command;
                    serialPort.Write(commandT, 0, commandT.Length);
                }
            }
        }

        public class FSE1001
        {
            public UInt32 Timestamp { get; set; }
            public float ForceZ { get; set; }
            public FSE1001(UInt32 timestamp = 0, float forceZ = 0)
            {
                Timestamp = timestamp;
                ForceZ = forceZ;
            }
            public void Print()
            {
                Console.WriteLine(String.Format("{0:0}: {1,5:0.0}", Timestamp, ForceZ));
            }
        }

        public class SerialThreadFSE1001 : SerialThread
        {
            public void ReadSerial(ref bool success, ref uint timestamp, ref float forceZ)
            {
                success = false;
                try
                {
                    //Initialize a buffer to hold the received data 
                    buffer = new byte[serialPort.ReadBufferSize];
                    int bytesRead = serialPort.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 1)//check how many bytes are read
                    {
                        bytesRead = serialPort.Read(buffer, 1, buffer.Length - 1);
                    }
                    if (buffer[0] == 0x0D && buffer[1] == 0x0C && buffer[11] == 0xFF) //check for data correctness
                    {
                        timestamp = ((uint)buffer[3] << 24)
                                        | (((uint)buffer[4]) << 16)
                                        | (((uint)buffer[5]) << 8)
                                        | (((uint)buffer[6]));
                        forceZ = BitConverter.ToSingle(new byte[4] { buffer[10], buffer[9], buffer[8], buffer[7] }, 0);
                        //Console.WriteLine(String.Format("{0:0} - {1} : {2,5:0.0}", timestamp, ByteArrayToString(buffer), forceZ));
                        success = true;
                    }
                }
                catch (TimeoutException)
                {
                    // do nothing
                }
            }

            public override void ThreadLoop()
            {
                // Opens the connection on the serial port
                serialPort.Open();
                //serialPort.Write(new char[] { 'z' }, 0, 1); // initialize the force sensor
                bool success = false;
                uint timestamp = 0;
                float forceZ = 0;

                // Looping
                while (IsLooping())
                {
                    // Send to Serial
                    if (transmitQueue.Count != 0)
                    {
                        var command = transmitQueue.Dequeue();
                        WriteToSerial(command);
                    }
                    // Read from Serial
                    ReadSerial(ref success, ref timestamp, ref forceZ);
                    if (success)
                    {
                        //Console.WriteLine(String.Format("{0:0}: {1,5:0.0}", timestamp, forceZ));
                        receiveQueue.Enqueue(new FSE1001(timestamp, forceZ));
                    }
                }
                serialPort.Close();
            }

            public void InitializeSensor()
            {
                transmitQueue.Enqueue(new char[] { 'z' });// initialize the force sensor
                Console.WriteLine("FSE1001 zeroed!");
            }
        }

}
