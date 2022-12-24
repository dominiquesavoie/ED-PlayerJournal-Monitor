using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using Newtonsoft.Json;

namespace ED_PlayerJournal_Monitor
{
    public enum MessageColor
    {
        Red,
        Blue,
        Green,
        Yellow
    }
    public static class ColorExtensions
    {
        public static string ToLogColor(this MessageColor color)
        {
            return color.ToString().ToLower();
        }
    }
    public class VoiceAttackPlugin
    {

        private static JournalMonitor _journalMonitor;
		public static string VA_DisplayName()
		{
			return "VoiceAttack ED Player Journal Monitor Plugin";  //a name to distinguish my plugin from others
		}

		public static Guid VA_Id()
		{
			return new Guid("{c7264e9a-aafa-498a-a7a6-1b3b1ca90bcc}");   //note this is a new guid for this plugin
		}

		public static string VA_DisplayInfo()
		{
			return "VoiceAttack ED Player Journal Monitor.\r\n\r\nReads and exposes every events from the Player Journal directly to Voice Attack.\r\n\r\nCreated by dominiquesavoie (2022)";  //this is just extended info that you might want to give to the user.  note that you should format this up properly.
		}

		public static void VA_Init1(dynamic vaProxy)
		{
            _journalMonitor = new JournalMonitor();
            _journalMonitor.JournalEvent += (sender, args) =>
            {
                HandleEvents(args, ref vaProxy);
            };

            var configThread = new Thread(_journalMonitor.Start);
            configThread.SetApartmentState(ApartmentState.STA);
            configThread.Start();

            vaProxy.WriteToLog($"{VA_DisplayName()} ready!", MessageColor.Green.ToLogColor());
        }

        private const string CommandNamePattern = "((EDPlayerJournal_{0}))";
        private static void HandleEvents(JournalEventArgs @event, ref dynamic vaProxy)
        {
            var eventType = @event.Event.First(x => x.Key == "event");
            var vaCommandName =  string.Format(CommandNamePattern, ((string)eventType.Value));
            if (!vaProxy.CommandExists(vaCommandName))
            {
                // No commands configured in VoiceAttack.
                return;
            }

            foreach (var eventElement in @event.Event)
            {
                switch (eventElement.Key)
                {
                    case "event":
                        continue;
                    default:
                        SetVariable(ref vaProxy, (string)eventType.Value,eventElement.Key, eventElement.Value);
                        break;
                };
            }
            
            vaProxy.ExecuteCommand(vaCommandName);
            
        }

        private static void SetVariable(ref dynamic vaProxy,string eventName, string variableName, object value)
        {
            if (value is IEnumerable<KeyValuePair<string, object>> enumerableValue)
            {
                // Convert arrays and lists to Json.
                var arrayOfItems = enumerableValue.ToArray();
                vaProxy.SetText($"EDPlayerJournal_{eventName}_{variableName}", JsonConvert.SerializeObject(arrayOfItems));
                return;
            }
            var stringValue = value.ToString();
            if (int.TryParse(stringValue, out var intValue))
            {
                vaProxy.SetInt($"EDPlayerJournal_{eventName}_{variableName}", intValue);
                return;
            }

            if (decimal.TryParse(stringValue, out var decValue))
            {
                vaProxy.SetDecimal($"EDPlayerJournal_{eventName}_{variableName}", decValue);
                return;

            }
            if (bool.TryParse(stringValue, out var boolValue))
            {
                vaProxy.SetBoolean($"EDPlayerJournal_{eventName}_{variableName}", boolValue);
            }

            vaProxy.SetText($"EDPlayerJournal_{eventName}_{variableName}", stringValue);
        }

        public static void VA_Exit1(dynamic vaProxy)
        {
            _journalMonitor.Stop();
        }

        public static void VA_StopCommand()
        {
            //no need to monitor this
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
            // no need to do this.
        }
    }
}
