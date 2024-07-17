using LiveSplit.BarnyardSplits;
using LiveSplit.Model;
using LiveSplit.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public class BarnyardComponent : LogicComponent
    {
        public BarnyardSettings Settings { get; set; }

        protected LiveSplitState CurrentState { get; set; }

        public override string ComponentName => "Barnyard Autosplitter";

        private Thread PipeThread;
        private TimerModel Timer;

        public BarnyardComponent(LiveSplitState state)
        {
            Timer = new TimerModel();
            Timer.CurrentState = state;

            Settings = new BarnyardSettings();

            PipeThread = new Thread(PipeThreadFunc);
            PipeThread.Start();

            CurrentState = state;
        }


        private NamedPipeClientStream pipe = new NamedPipeClientStream(
            ".",
            "BYSpeedrunHelper",
            PipeAccessRights.ReadData,
            PipeOptions.None,
            System.Security.Principal.TokenImpersonationLevel.None,
            System.IO.HandleInheritability.None
        );

        private CancellationTokenSource cts = new CancellationTokenSource();

        private TimeSpan StrToTimeSpan(string str)
        {
            var components = str.Split('_');

            var hours = Convert.ToInt32(components[0]);
            var minutes = Convert.ToInt32(components[1]);
            var seconds = Convert.ToInt32(components[2]);
            var milliseconds = Convert.ToInt32(components[3]);

            return new TimeSpan(hours / 24, hours % 24, minutes, seconds, milliseconds);
        }

        private void ParseMessage(byte[] buf)
        {
            uint numOfMessages = BitConverter.ToUInt32(buf, 4);
            uint numReadMessages = 0;
            int currentByte = 8;

            while (numReadMessages < numOfMessages)
            {
                int msgLen = BitConverter.ToInt32(buf, currentByte);
                currentByte += 4;

                if (buf[currentByte] == ';')
                {
                    currentByte++;
                }
                else if (buf[currentByte] == '\0')
                {
                    Debug.Print("Unexpected end of stream");
                    break;
                }

                switch (buf[currentByte])
                {
                    case (byte)'1':
                        // start run
                        Timer.Reset();
                        Timer.Start();
                        break;
                    case (byte)'2':
                        // end run
                        string endGameTime = System.Text.Encoding.UTF8.GetString(buf, currentByte + 1, msgLen - 1);
                        CurrentState.SetGameTime(StrToTimeSpan(endGameTime));
                        Timer.Split();
                        break;
                    case (byte)'3':
                        // reset
                        Timer.Reset();
                        break;
                    case (byte)'4':
                        // resume
                        Timer.UndoAllPauses();
                        break;
                    case (byte)'5':
                        // pause
                        Timer.Pause();
                        break;
                    case (byte)'6':
                        // split
                        string curGameTime = System.Text.Encoding.UTF8.GetString(buf, currentByte + 1, msgLen - 1);
                        CurrentState.SetGameTime(StrToTimeSpan(curGameTime));
                        Timer.Split();
                        break;
                    case (byte)'7':
                        // set in game time
                        string syncGameTime = System.Text.Encoding.UTF8.GetString(buf, currentByte + 1, msgLen - 1);
                        CurrentState.SetGameTime(StrToTimeSpan(syncGameTime));
                        break;
                    case (byte)'8':
                        // loading started
                        CurrentState.IsGameTimePaused = true;
                        break;
                    case (byte)'9':
                        // loading ended
                        CurrentState.IsGameTimePaused = false;
                        break;
                    default:
                        Debug.Print("Got an unknown message!");
                        break;
                }

                currentByte += msgLen;
                numReadMessages += 1;
            }
        }

        private void PipeThreadFunc()
        {
            while (!cts.IsCancellationRequested)
            {
                if (!pipe.IsConnected)
                {
                    Debug.WriteLine("Connecting to the pipe.");

                    try
                    {
                        pipe.Connect(0);
                        Debug.WriteLine("Connected to the pipe. Readmode: " + pipe.ReadMode);
                    }
                    catch (Exception e) when (e is TimeoutException || e is IOException)
                    {
                        Debug.WriteLine("Idling for 1 second.");
                        cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                        continue;
                    }
                }
                
                try
                {
                    var buf = new byte[65535];
                    var task = pipe.ReadAsync(buf, 0, 65535, cts.Token);
                    task.Wait();

                    if (task.Result == 0)
                    {
                        // The pipe was closed.
                        Debug.WriteLine("Pipe end of stream reached.");
                        continue;
                    }

                    int expectedNumOfBytes = BitConverter.ToInt32(buf, 0);

                    if (expectedNumOfBytes != task.Result)
                    {
                        Debug.WriteLine("Received an incorrect number of bytes (" + task.Result + ", expected " + expectedNumOfBytes + ").");
                        continue;
                    }

                    ParseMessage(buf);
                }
                catch (AggregateException e)
                {
                    foreach (var ex in e.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            return;
                        }
                    }

                    Debug.WriteLine("Error reading from the pipe:");
                    foreach (var ex in e.InnerExceptions)
                    {
                        Debug.WriteLine("- " + ex.GetType().Name + ": " + ex.Message);
                    }
                    Debug.WriteLine("Idling for 1 second.");
                    cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    continue;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error reading from the pipe: " + e.Message);
                    Debug.WriteLine("Idling for 1 second.");
                    cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    continue;
                }
            }
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {

        }

        public override void Dispose()
        {
            cts.Cancel();
            PipeThread.Abort();
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return Settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

    }
}