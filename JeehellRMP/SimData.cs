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
using System.Windows;
using System.Windows.Interop;

namespace JeehellRMP
{
    class SimData
    {
        public event EventHandler DataUpdated;
        public event Action<string> ConnectedToFs;
        public event Action DisconnectedFromFs;

        public static SimData GetInstance()
        {
            if (instance == null)
            {
                instance = new SimData();
            }
            return instance;
        }

        public static bool isConnectedToFs
        {
            get
            {
                SimConnect simconnect = GetInstance().simconnect;
                return simconnect != null;
            }
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

        public string Com2ActiveFreq
        {
            get
            {
                return FrequencyFromBcd(fsData.COM2_ACT_Frequency);
            }
        }

        public string Com2StandbyFreq
        {
            get
            {
                return FrequencyFromBcd(fsData.COM2_STB_Frequency);
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
            RmpData.ActiveModeChanged += RmpData_ActiveModeChanged;

            HookSimconnect(Application.Current.MainWindow);

            OnDisconnectedFromFs();
        }

        /// <summary>
        /// Figures out the correct way to hook simconnect messaging to a window
        /// </summary>
        /// <param name="window">Window to hook</param>
        private void HookSimconnect(Window window)
        {
            if (window.IsLoaded)
            {
                attachMessageHook(window);
                return;
            }
            window.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Attaches the simconnect message hook to a window
        /// </summary>
        /// <param name="window">Window to attach the hook</param>
        private void attachMessageHook(Window window)
        {
            var Window = new WindowInteropHelper(window);
            HwndSource source = HwndSource.FromHwnd(Window.Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            attachMessageHook(sender as Window);
        }

        /// <summary>
        /// Handles Win32 Windows Messaging for communicating with SimConnect
        /// 
        /// To get data from SimConnect you either poll at fixed intervals (bad) or you use the "old" windows
        /// messaging to let SimConnect signal you when it has something to say. To do this you need to pass the handle
        /// of the window that will receive the messages when setting up a connection with simconnect and handle that 
        /// reception in that window (traditionally by overriding DefWndProc).
        /// 
        /// WPF doesn't want anything to do with the Win32 past so all window messaging is gone. Fortunately 
        /// class System.Windows.Interop is there to bring back these features.
        /// 
        /// TODO: Investigate if it is possible/worth it to create a (hidden) traditional W32 window and have that receive messages
        /// </summary>
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_USER_SIMCONNECT)
            {
                ReceiveSimconnectMessage();
            }
            return IntPtr.Zero;
        }

        private void RmpData_ActiveModeChanged(RmpData.RmpMode obj)
        {
            OnDataUpdated();
        }

        private void AttemptFsConnection_DoWork(object sender, DoWorkEventArgs e)
        {
            while (isConnectedToFs == false)
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
            COM2_RADIO_SWAP,
            COM2_RADIO_WHOLE_DEC,
            COM2_RADIO_WHOLE_INC,
            COM2_RADIO_FRACT_DEC,
            COM2_RADIO_FRACT_INC,
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
            public double COM2_ACT_Frequency;
            public double COM2_STB_Frequency;
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
        /// Signals the internal SimConnect method to process data from FS
        /// 
        /// Note: SimData is a singleton therefor simconnect is only created once as well too.
        /// </summary>
        internal static void ReceiveSimconnectMessage()
        {
            if (isConnectedToFs == false) return;
            SimConnect simconnect = SimData.GetInstance().simconnect;

            try
            {
                simconnect.ReceiveMessage();
            }
            catch (COMException ex)
            {
                //If any exception happens here we can be pretty sure simconnect is gone so:
                DisposeSimconnect();

                Debug.WriteLine("MainWindow:COMException {0}:{1}", ex.ErrorCode, ex.Message);
            }
        }

        /// <summary>
        /// Attempts a connection to FS and sets up events, handlers and data communication
        /// 
        /// Note: Attempts to get the handle to the current MainWindow, where a message hook needs to be setup for messages to arrive.
        /// </summary>
        /// <returns>True if it connects. If unsuccessfull simconnect is null.</returns>
        private bool ConnectToFs()
        {
            if (simconnect != null) return true;

            IntPtr mainWindowHandle = GetMainWindowHandle();
            if (mainWindowHandle == IntPtr.Zero) return false;
            
            try
            {
                simconnect = new SimConnect("JeehellRMP", mainWindowHandle, WM_USER_SIMCONNECT, null, SimConnect.SIMCONNECT_OPEN_CONFIGINDEX_LOCAL);

                simconnect.OnRecvOpen += Simconnect_OnRecvOpen;
                simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;
                simconnect.OnRecvQuit += Simconnect_OnRecvQuit;
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

        /// <summary>
        /// Gets the MainWindow Handle
        /// </summary>
        /// <remarks>
        /// Dispatcher is used because Application.Current.MainWindow only works in the same thread as MainWindow
        /// (https://msdn.microsoft.com/en-us/library/system.windows.application.mainwindow(v=vs.110).aspx)
        /// </remarks>
        /// <returns>Handle of MainWindow</returns>
        private IntPtr GetMainWindowHandle()
        {
            IntPtr mainWindowHandle = IntPtr.Zero;
            WindowInteropHelper mainWindow;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow = new WindowInteropHelper(Application.Current.MainWindow);
                if (mainWindow != null) mainWindowHandle = mainWindow.Handle;
            });

            return mainWindowHandle;
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            DisposeSimconnect();
        }

        private static void DisposeSimconnect()
        {
            if (isConnectedToFs == false) return;

            SimData simdata = GetInstance();
            simdata.simconnect.Dispose();
            simdata.simconnect = null;
            simdata.OnDisconnectedFromFs();
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
                simconnect.MapClientEventToSimEvent(Event.COM2_RADIO_SWAP, "COM2_RADIO_SWAP");
                simconnect.MapClientEventToSimEvent(Event.COM2_RADIO_FRACT_DEC, "COM2_RADIO_FRACT_DEC");
                simconnect.MapClientEventToSimEvent(Event.COM2_RADIO_FRACT_INC, "COM2_RADIO_FRACT_INC");
                simconnect.MapClientEventToSimEvent(Event.COM2_RADIO_WHOLE_DEC, "COM2_RADIO_WHOLE_DEC");
                simconnect.MapClientEventToSimEvent(Event.COM2_RADIO_WHOLE_INC, "COM2_RADIO_WHOLE_INC");

                simconnect.AddClientEventToNotificationGroup(NotificationGroup.Default, Event.COM_RADIO_SWAP, false);
                simconnect.SetNotificationGroupPriority(NotificationGroup.Default, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);

                simconnect.AddToDataDefinition(DataDefinition.SimConnectDataStructure, "COM ACTIVE FREQUENCY:1", null, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DataDefinition.SimConnectDataStructure, "COM STANDBY FREQUENCY:1", null, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DataDefinition.SimConnectDataStructure, "COM ACTIVE FREQUENCY:2", null, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DataDefinition.SimConnectDataStructure, "COM STANDBY FREQUENCY:2", null, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

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
        /// </summary>
        /// <remarks>
        /// We're not using the "tagged data" feature of SimConnect so the whole structure is being sent from FS every time a member changes.
        /// While tagged data are more economic for big sets, it is also quite more complicated to implement it so this will do as fine for this use case.
        /// 
        /// TODO: Implement tagged data (for the fun of it)
        /// </remarks>
        /// <exception cref="InvalidCastException">When the received data structure is unknown</exception>
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
            if (isConnectedToFs == false) return;

            SimConnect simconnect = GetInstance().simconnect;

            switch (RmpData.ActiveMode)
            {
                case RmpData.RmpMode.VHF1:
                    simconnect.TransmitClientEvent(0, Event.COM_RADIO_SWAP, 0, NotificationGroup.Default, SIMCONNECT_EVENT_FLAG.DEFAULT);
                    break;
                case RmpData.RmpMode.VHF2:
                    simconnect.TransmitClientEvent(0, Event.COM2_RADIO_SWAP, 0, NotificationGroup.Default, SIMCONNECT_EVENT_FLAG.DEFAULT);
                    break;
            }
        }

        /// <summary>
        /// Handles sending the appropriate event to FS for each outer/inner knob turned counter/clockwise
        /// </summary>
        /// <param name="knob">The inner or outer knob turned</param>
        /// <param name="knobDirection">The direction of the knob</param>
        internal static void KnobTurned(Knob knob, KnobDirection knobDirection)
        {
            if (isConnectedToFs == false) return;

            SimConnect simconnect = SimData.GetInstance().simconnect;

            //Initializing to stupid default to keep the compiler from complaining variable might be used uninitialized.
            Event eventToTransmit = Event.COM_RADIO_WHOLE_DEC;

            switch (knob)
            {
                case Knob.InnerKnob:
                    if (knobDirection == KnobDirection.Clockwise)
                    {
                        if (RmpData.ActiveMode == RmpData.RmpMode.VHF1) eventToTransmit = Event.COM_RADIO_FRACT_INC;
                        if (RmpData.ActiveMode == RmpData.RmpMode.VHF2) eventToTransmit = Event.COM2_RADIO_FRACT_INC;
                        break;
                    }
                    if (RmpData.ActiveMode == RmpData.RmpMode.VHF1) eventToTransmit = Event.COM_RADIO_FRACT_DEC;
                    if (RmpData.ActiveMode == RmpData.RmpMode.VHF2) eventToTransmit = Event.COM2_RADIO_FRACT_DEC;
                    break;
                case Knob.OuterKnob:
                    if (knobDirection == KnobDirection.Clockwise)
                    {
                        if (RmpData.ActiveMode == RmpData.RmpMode.VHF1) eventToTransmit = Event.COM_RADIO_WHOLE_INC;
                        if (RmpData.ActiveMode == RmpData.RmpMode.VHF2) eventToTransmit = Event.COM2_RADIO_WHOLE_INC;
                        break;
                    }
                    if (RmpData.ActiveMode == RmpData.RmpMode.VHF1) eventToTransmit = Event.COM_RADIO_WHOLE_DEC;
                    if (RmpData.ActiveMode == RmpData.RmpMode.VHF2) eventToTransmit = Event.COM2_RADIO_WHOLE_DEC;
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

        private void OnDisconnectedFromFs()
        {
            if (DisconnectedFromFs == null) return;

            DisconnectedFromFs();
            AttemptFsConnection.RunWorkerAsync();
        }
    }
}