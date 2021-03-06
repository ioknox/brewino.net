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
    public struct BoardSettings : ISettings
    {
        public string Filename { get; set; }
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public Handshake Handshake { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
        public int WriteTimeout { get; set; }
        public int ReadTimeout { get; set; }
        public int ReadBufferSize { get; set; }
        public int WriteBufferSize { get; set; }

        public static BoardSettings Default
        {
            get
            {
                return new BoardSettings
                {
                    Filename = "serial.json",
                    PortName = "/dev/ttyACM0",
                    BaudRate = 115200,
                    Handshake = Handshake.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    WriteTimeout = 10,
                    ReadTimeout = 10,
                    ReadBufferSize = 64,
                    WriteBufferSize = 64
                };
            }
        }
    }
    
}
