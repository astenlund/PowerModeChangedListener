using System;
using System.Management;
using System.Threading;
using Microsoft.Win32;

namespace PowerModeChangedListener
{
    public static class Program
    {
        private static readonly object Locker = new();
        private static readonly ManagementEventWatcher EventWatcher = new() { Query = new WqlEventQuery("Win32_PowerManagementEvent") };

        public static void Main()
        {
            Start();
            Wait();
            Stop();
            ClearConsoleBuffer();
            PressAnyKeyToContinue();
        }

        private static void Start()
        {
            SystemEvents.PowerModeChanged += SystemEventsOnPowerModeChanged;
            SystemEvents.EventsThreadShutdown += SystemEventsOnEventsThreadShutdown;
            SystemEvents.SessionEnded += SystemEventsOnSessionEnded;
            SystemEvents.SessionSwitch += SystemEventsOnSessionSwitch;
            EventWatcher.EventArrived += EventWatcherOnEventArrived;
            EventWatcher.Start();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            TimeStampedPrint("Event listeners started");
        }

        private static void Wait()
        {
            lock (Locker)
            {
                Monitor.Wait(Locker);
            }
        }

        private static void Stop()
        {
            EventWatcher.Stop();
            Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
            EventWatcher.EventArrived -= EventWatcherOnEventArrived;
            SystemEvents.SessionSwitch -= SystemEventsOnSessionSwitch;
            SystemEvents.SessionEnded -= SystemEventsOnSessionEnded;
            SystemEvents.EventsThreadShutdown -= SystemEventsOnEventsThreadShutdown;
            SystemEvents.PowerModeChanged -= SystemEventsOnPowerModeChanged;
            TimeStampedPrint("Event listeners stopped");
        }

        private static void ClearConsoleBuffer()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }

        private static void PressAnyKeyToContinue()
        {
            Console.Write("\nPress any key to continue . . . ");
            Console.ReadKey(true);
        }

        private static void SystemEventsOnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            TimeStampedPrint("SystemEvents.PowerModeChanged: {0}", e.Mode);
        }

        private static void SystemEventsOnEventsThreadShutdown(object sender, EventArgs eventArgs)
        {
            TimeStampedPrint("SystemEvents.EventsThreadShutdown");
        }

        private static void SystemEventsOnSessionEnded(object sender, SessionEndedEventArgs e)
        {
            TimeStampedPrint("SystemEvents.SessionEnded: {0}", e.Reason);
        }

        private static void SystemEventsOnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            TimeStampedPrint("SystemEvents.SessionSwitch: {0}", e.Reason);
        }

        private static void EventWatcherOnEventArrived(object sender, EventArrivedEventArgs e)
        {
            var eventType = Convert.ToInt32(e.NewEvent.Properties["EventType"].Value);
            switch (eventType)
            {
                case 4:
                    TimeStampedPrint("Win32_PowerManagementEvent: Entering Suspend");
                    break;
                case 7:
                    TimeStampedPrint("Win32_PowerManagementEvent: Resume From Suspend");
                    break;
                case 10:
                    TimeStampedPrint("Win32_PowerManagementEvent: Power Status Change");
                    break;
                case 11:
                    TimeStampedPrint("Win32_PowerManagementEvent: OEM Event");
                    break;
                case 18:
                    TimeStampedPrint("Win32_PowerManagementEvent: Resume Automatic");
                    break;
                default:
                    TimeStampedPrint("Win32_PowerManagementEvent: {0}", eventType);
                    break;
            }
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                e.Cancel = true;
            }

            lock (Locker)
            {
                Monitor.Pulse(Locker);
            }
        }

        private static void TimeStampedPrint(string format, params object[] args)
        {
            Console.WriteLine("{0:HH:mm:ss:fff} {1}", DateTime.Now, string.Format(format, args));
        }
    }
}
