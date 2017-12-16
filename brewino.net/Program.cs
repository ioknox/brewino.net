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


    public interface ISettings
    {
        string Filename { get; }
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



    class MainClass
    {
        private static AutoResetEvent ReceiveEvent = new AutoResetEvent(false);

        public static void Dump<TObject>(TObject value)
        {
            var data = MessagePackSerializer.Serialize(value);
            Console.WriteLine(MessagePackSerializer.ToJson(data));
            Console.WriteLine("({0}): {1}", data.Length, string.Join(" ", data.Select(p => p.ToString("X2"))));
            Console.WriteLine("---");
        }

        public static void Main(string[] args)
        {
            //var settings = SerialSettings.Default;
            //
            //if (!settings.Load())
            //{
            //    settings.Save();
            //}


            var mc = new Status();

            var commands = new List<ICommand>
            {
                new SendReportCommand(),
                new SetPinCommand(PinEnum.BoilTankPower, 7, 1),
                new SetPinCommand(PinEnum.BoilTankPower, 7, 0),
                new SetPinCommand(PinEnum.ValveCitySewer, 8, 1),
                new SetPinCommand(PinEnum.MotorPumpSpeed, 9, 345),
                new SetTankLevelCommand(TankEnum.BoilTank, 0)                
            };

            commands.ForEach(Dump);
            Dump(mc as ICommand);

            var board = new Board();

            board.Connect();

            while (true)
            {
                var watch = new Stopwatch();

                watch.Start();
                var received = board.SendAsync(commands).Result;
                watch.Stop();

                Console.WriteLine("{0} ms", watch.ElapsedMilliseconds);

                foreach (var cmd in received)
                {
                    Console.WriteLine(MessagePackSerializer.ToJson(cmd));
                }
            }

            /*
            while (true)
            {
                try
                {
                    using (var port = new SerialPort("/dev/ttyACM0", 115200))
                    {   
                        port.Handshake = Handshake.None;
                        port.DataBits = 8;
                        port.StopBits = StopBits.One;
                        port.Parity = Parity.None;
                        port.WriteTimeout = 10;
                        port.ReadTimeout = 10;
                        port.ReadBufferSize = 64;
                        port.WriteBufferSize = 64;
                        port.Open();

                        var collection = new BlockingCollection<ICommand>();

                        using (var reader = new Receiver(port))
                        {
                            reader.CommandEvent += collection.Add;

                            while (port.IsOpen)
                            {
                                foreach (var cmd in commands)
                                {
                                    var data = MessagePackSerializer.Serialize(cmd);
                                    Console.WriteLine("TX {0} ({1}): {2}", cmd.GetType().Name, data.Length, string.Join(" ", data.Select(p => p.ToString("X2"))));
                                    port.Write(data, 0, data.Length);

                                    try
                                    {
                                        var reply = collection.Take(new CancellationTokenSource(1000).Token);
                                        Console.WriteLine("{0} -> {1}", DateTime.UtcNow.Millisecond, MessagePackSerializer.ToJson(reply));
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        Console.WriteLine("Timout");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FAILED TO OPEN PORT");
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    Thread.Sleep(10);
                }
            }
            /*
			// call Serialize/Deserialize, that's all.
            var bytes = MessagePackSerializer.Serialize(mc);
            var mc2 = MessagePackSerializer.Deserialize<Status<TankRead>>(bytes);

			// you can dump msgpack binary to human readable json.
			// In default, MeesagePack for C# reduce property name information.
			// [99,"hoge","huga"]
			var json = MessagePackSerializer.ToJson(bytes);
            Console.WriteLine(string.Join(" ", bytes.Select(p => p.ToString("X2"))));*/
        }
    }
}
