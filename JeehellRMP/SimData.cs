using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JeehellRMP
{
    class SimData
    {
        public event EventHandler DataUpdated;
        public event Action<string> ConnectedToFs;

        public static SimData GetInstance()
        {
            if (instance == null)
            {
                instance = new SimData();
            }
            return instance;
        }

        public string Com1ActiveFreq
        {
            get
            {
                return FrequencyFromBcd(fsData.COM1_ACT_Frequency);
            }
        }
        public string Com1StandbyFreq
        {
            get
            {
                return FrequencyFromBcd(fsData.COM1_STB_Frequency);
            }
        }

        private static SimData instance;
        private SimConnect simconnect;
        internal const int WM_USER_SIMCONNECT = 0x0402;
        private FstDataStructure fsData;
        BackgroundWorker AttemptFsConnection;

        private SimData()
        {
            AttemptFsConnection = new BackgroundWorker();
            AttemptFsConnection.DoWork += AttemptFsConnection_DoWork;
            AttemptFsConnection.RunWorkerAsync();
        }

        private void AttemptFsConnection_DoWork(object sender, DoWorkEventArgs e)
        {
            while (simconnect == null)
            {
                if (ConnectToFs() == false) Thread.Sleep(1000);
            }
        }

        private enum NotificationGroup
        {
            Default,
        }

        private enum DataRequest
        {
            Fs,
        }

        private enum Event
        {
            COM_RADIO_SWAP,
            COM_RADIO_WHOLE_DEC,
            COM_RADIO_WHOLE_INC,
            COM_RADIO_FRACT_DEC,
            COM_RADIO_FRACT_INC,
        };

        private enum DataDefinition
        {
            SimConnectDataStructure,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FstDataStructure
        {
            public double COM1_ACT_Frequency;
            public double COM1_STB_Frequency;
        }

        public enum Knob
        {
            OuterKnob,
            InnerKnob,
        }
        public enum KnobDirection
        {
            Clockwise,
            CounterClockWise,
        }

        /// <summary>
        /// Signals internal SimConnect method to process data from FS
        /// 
        /// TODO: There doesn't seem to be a good way to attach an internal class to a XAML event so instead it is
        /// made static to allow it to be easilly accessed from outside the class, without complicating things too much.
        /// If a better mechanism is available this should be revised
        /// 
        /// Note: SimData is a singleton therefor simconnect is too.
        /// </summary>
        internal static void ReceiveMessage()
        {
            SimConnect simconnect = SimData.GetInstance().simconnect;
            if (simconnect == null) return;
            simconnect.ReceiveMessage();
        }

        /// <summary>
        /// Attempts a connection to FS and sets up events, handlers and data communication
        /// 
        /// TODO: Fix the window messaging mess
        /// </summary>
        /// <returns>True if it connects. If unsuccessfull simconnect is null.</returns>
        private bool ConnectToFs()
        {
            if (simconnect != null) return true;

            IntPtr mainWindowHandle = IntPtr.Zero;
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                if (MainWindow.Window != null) mainWindowHandle = MainWindow.Window.Handle;
            });

            if (mainWindowHandle == IntPtr.Zero) return false;

            try
            {
                simconnect = new SimConnect("AimForFS", mainWindowHandle, WM_USER_SIMCONNECT, null, 0);

                simconnect.OnRecvOpen += Simconnect_OnRecvOpen;
                simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;
                SimConnectSetup();
                return true;
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode != -2147467259) //Ignore the typical E_FAIL when FS isn't running
                {
                    Debug.WriteLine("ConnectToFS(): COMException {0}: {1}", ex.ErrorCode, ex.Message);
                }
            }
            return false;
        }

        private void SimConnectSetup()
        {
            try
            {
                simconnect.MapClientEventToSimEvent(Event.COM_RADIO_SWAP, "COM_STBY_RADIO_SWAP");
                simconnect.MapClientEventToSimEvent(Event.COM_RADIO_FRACT_DEC, "COM_RADIO_FRACT_DEC");
                simconnect.MapClientEventToSimEvent(Event.COM_RADIO_FRACT_INC, "COM_RADIO_FRACT_INC");
                simconnect.MapClientEventToSimEvent(Event.COM_RADIO_WHOLE_DEC, "COM_RADIO_WHOLE_DEC");
                simconnect.MapClientEventToSimEvent(Event.COM_RADIO_WHOLE_INC, "COM_RADIO_WHOLE_INC");

                simconnect.AddClientEventToNotificationGroup(NotificationGroup.Default, Event.COM_RADIO_SWAP, false);
                simconnect.SetNotificationGroupPriority(NotificationGroup.Default, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);

                simconnect.AddToDataDefinition(DataDefinition.SimConnectDataStructure, "COM ACTIVE FREQUENCY:1", null, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DataDefinition.SimConnectDataStructure, "COM STANDBY FREQUENCY:1", null, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.RegisterDataDefineStruct<FstDataStructure>(DataDefinition.SimConnectDataStructure);
            }
            catch (COMException ex)
            {
                Debug.WriteLine("SimConnectSetup(): Exception {0}: {1}", ex.ErrorCode, ex.Message);
            }
        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Debug.WriteLine("Connected to " + data.szApplicationName);
            Debug.WriteLine("SimConnect:" + data.dwSimConnectVersionMajor + "." + data.dwSimConnectVersionMinor + " (" + data.dwSimConnectBuildMajor + "." + data.dwSimConnectBuildMinor + "." + data.dwVersion + ")");
            simconnect.RequestDataOnSimObject(DataRequest.Fs, DataDefinition.SimConnectDataStructure, 0, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
            OnConnectedToFs(data.szApplicationName);
        }

        /// <summary>
        /// Handles incoming data from FS.
        /// 
        /// Fires DataUpdated event to notify subscribers.
        /// 
        /// Note: We're not using the "tagged data" feature of SimConnect so the whole structure is being sent from FS every time a member changes.
        /// While tagged data are more economic for big sets, it is also quite more complicated to implement it so this will do as fine for this use case.
        /// 
        /// TODO: Implement tagged data (for the fun of it)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch ((DataRequest)data.dwRequestID)
            {
                case DataRequest.Fs:
                    fsData = (FstDataStructure)data.dwData[0];
                    OnDataUpdated();
                    break;
                default:
                    throw new InvalidCastException("Received unknown data structure");
            }
        }

        /// <summary>
        /// Format a BCD frequency (as sent from FS) to a string
        /// 
        /// TODO: Assumes that the provided double will have a decimal separator of "," or "." and blindly attempts
        /// to replace "," to ".". This will most certainly break in other cultures where the decimal separator is different
        /// A better solution needs to set an appropriate culture for the convertion.
        /// </summary>
        /// <param name="FrequencyBCD"></param>
        /// <returns>Frequency as formatted string</returns>
        public static string FrequencyFromBcd(double FrequencyBCD)
        {
            return string.Format("{0:000.000}", FrequencyBCD / 1000000).Replace(",", ".");
        }

        internal static void TransferKeyPressed()
        {
            SimConnect simconnect = SimData.GetInstance().simconnect;
            if (simconnect == null) return;

            simconnect.TransmitClientEvent(0, Event.COM_RADIO_SWAP, 0, NotificationGroup.Default, SIMCONNECT_EVENT_FLAG.DEFAULT);
        }

        /// <summary>
        /// Handles sending the appropriate event to FS for each outer/inner knob turned counter/clockwise
        /// </summary>
        /// <param name="knob">The inner or outer knob turned</param>
        /// <param name="knobDirection">The direction of the knob</param>
        internal static void KnobTurned(Knob knob, KnobDirection knobDirection)
        {
            SimConnect simconnect = SimData.GetInstance().simconnect;
            if (simconnect == null) return;

            Event eventToTransmit;

            switch (knob)
            {
                case Knob.InnerKnob:
                    if (knobDirection == KnobDirection.Clockwise)
                    {
                        eventToTransmit = Event.COM_RADIO_FRACT_INC;
                        break;
                    }
                    eventToTransmit = Event.COM_RADIO_FRACT_DEC;
                    break;
                case Knob.OuterKnob:
                    if (knobDirection == KnobDirection.Clockwise)
                    {
                        eventToTransmit = Event.COM_RADIO_WHOLE_INC;
                        break;
                    }
                    eventToTransmit = Event.COM_RADIO_WHOLE_DEC;
                    break;
                default:
                    //Stupid default to keep the compiler from complaining eventToTransmit might be used uninitialized.
                    eventToTransmit = Event.COM_RADIO_WHOLE_DEC;
                    break;
            }
            simconnect.TransmitClientEvent(0, eventToTransmit, 0, NotificationGroup.Default, SIMCONNECT_EVENT_FLAG.DEFAULT);
        }

        private void OnDataUpdated()
        {
            if (DataUpdated == null) return;

            DataUpdated(this, EventArgs.Empty);
        }

        private void OnConnectedToFs(string FlightSimulatorName)
        {
            if (ConnectedToFs == null) return;

            ConnectedToFs(FlightSimulatorName);
        }
    }
}