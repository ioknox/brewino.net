using System;
using System.Linq;
using MessagePack;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
namespace brewino.net
{
    /*
    public static class Extensions
    {
        public static int IndexOf<TData>(this IEnumerable<TData> self, TData value)
            where TData : IComparable<TData>
        {
            return self.IndexOf(p => p.CompareTo(value) == 0);
        }

        public static int IndexOf<TData>(this IEnumerable<TData> self, Func<TData, bool> functor)
            where TData : IComparable<TData>
        {
            return self.Where(functor).Select((p, i) => (int?)i).FirstOrDefault() ?? -1;
        }
    }
*/
    
    /*
    public static class SettingsExtensions
    {
        public static bool Load<TData>(this TData self)
            where TData : struct, ISettings
        {
            try
            {
                if (!File.Exists(self.Filename))
                {
                    return false;
                }

                var ser = new JsonSerializer();

                using (var fileStream = new StreamReader(self.Filename))
                using (var jsonReader = new JsonTextReader(fileStream))
                {
                    self = ser.Deserialize<TData>(jsonReader);
                }

                return true;
            }
            catch
            {
                // Nothing to do...
            }

            return false;
        }

        public static bool Save<TData>(this TData self)
            where TData : struct, ISettings
        {

            try
            {
                var ser = new JsonSerializer();

                using (var fileStream = new StreamWriter(self.Filename))
                using (var jsonReader = new JsonTextWriter(fileStream))
                {
                    ser.Serialize(jsonReader, self);
                }

                return true;
            }
            catch
            {
                // Nothing to do...
            }

            return false;
        }
    }
*/

    public class Board
    {
        private readonly object _locker = new object();

        private readonly Thread _thread;

        private readonly ManualResetEvent _runEvent = new ManualResetEvent(false);

        private readonly BlockingCollection<ICommand[]> _writeCollection = new BlockingCollection<ICommand[]>();

        private readonly BlockingCollection<ICommand[]> _readCollection = new BlockingCollection<ICommand[]>();

        private BoardSettings Settings { get; set; } = BoardSettings.Default;

        public Board()        
        {
            _thread = new Thread(Run)
            {
                Name = $"Serial port management and write thread for {Settings.PortName}"
            };
            _thread.Start();
        }

        public void Connect()
        {
            _runEvent.Set();
        }

        public void Disconnect()
        {
            _runEvent.Reset();
        }

        private void Run()
        {
            while (_runEvent.WaitOne())
            {
                try
                {
                    using (var port = new SerialPort(Settings.PortName, Settings.BaudRate))
                    {   
                        port.Handshake = Settings.Handshake;
                        port.DataBits = Settings.DataBits;
                        port.StopBits = Settings.StopBits;
                        port.Parity = Settings.Parity;
                        port.WriteTimeout = Settings.WriteTimeout;
                        port.ReadTimeout = Settings.ReadTimeout;
                        port.ReadBufferSize = Settings.ReadBufferSize;
                        port.WriteBufferSize = Settings.WriteBufferSize;

                        var timeout = (port.ReadTimeout + port.WriteTimeout) * 10;

                        try
                        {
                            port.Open();
                        }
                        catch
                        {
                            Console.WriteLine("Failed to open {0}", Settings.PortName);
                            Thread.Sleep(TimeSpan.FromSeconds(2));
                            throw;
                        }

                        using (var reader = new BoardReceiver(port))
                        {
                            while (port.IsOpen)
                            {
                                var commands = _writeCollection.Take();
                                var received = new List<ICommand>();

                                foreach (var cmd in commands)
                                {
                                    var data = MessagePackSerializer.Serialize(cmd);
                                    Console.WriteLine("TX {0} ({1}): {2}", cmd.GetType().Name, data.Length, string.Join(" ", data.Select(p => p.ToString("X2"))));
                                    port.Write(data, 0, data.Length);

                                    try
                                    {
                                        var tokenSource = new CancellationTokenSource(timeout);
                                        received.Add(reader.ReceiveCollection.Take(tokenSource.Token));
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        Console.WriteLine("Timeout");
                                    }
                                }

                                _readCollection.Add(received.ToArray());
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public async Task<ICommand[]> SendAsync(IEnumerable<ICommand> commands)
        {
            Task<ICommand[]> task;

            lock (_locker)
            {
                // Ensure to be the next to receive something
                task = Task.Run<ICommand[]>((Func<ICommand[]>)_readCollection.Take);

                // Trigger write thread by put something to send
                _writeCollection.Add(commands.ToArray());
            }

            return await task;
        }
    }
    
}
