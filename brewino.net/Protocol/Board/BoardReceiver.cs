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

    internal sealed class BoardReceiver : IDisposable
    {
        private CancellationTokenSource _token = new CancellationTokenSource();

        private readonly SerialPort _port;

        public readonly BlockingCollection<ICommand> ReceiveCollection = new BlockingCollection<ICommand>();

        public BoardReceiver(SerialPort port)
        {
            _port = port;

            var thread = new Thread(Run)
            {
                Name = "SerialPort receiver"
            };

            thread.Start();
        }

        private void Run()
        {
            var buf = new byte[_port.ReadBufferSize];

            while (!_token.IsCancellationRequested)
            {
                try
                {
                    var pos = 0;

                    try
                    {
                        do 
                        {
                            pos += _port.Read(buf, pos, _port.BytesToRead);
                        } while (pos + _port.BytesToRead < buf.Length);
                    }
                    catch (TimeoutException)
                    {
                        // Nothing to do...
                    }

                    if (pos > 0)
                    {   
                        var received = buf.TakeWhile((b, i) => i < pos).ToArray();
                        Console.WriteLine("RX ({0}): {1}", pos, string.Join(" ", received.Select(p => p.ToString("X2"))));
                        ReceiveCollection.Add(MessagePackSerializer.Deserialize<ICommand>(received));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Nothing to do...
                }
                catch (Exception ex)
                {
                    Console.WriteLine("err {0} {1}", ex.Source, ex.Message);
                }
            }
        }

        public void Dispose()
        {
            _token.Cancel();
        }
    }
    
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
    
}
