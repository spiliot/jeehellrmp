using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeehellRMP
{
    class RmpData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ActiveFreq
        {
            get { return _ActiveFreq; }
            set
            {
                _ActiveFreq = value;
                OnPropertyChanged("ActiveFreq");
            }
        }

        public string StandbyFreq
        {
            get { return _StandbyFreq; }
            private set
            {
                _StandbyFreq = value;
                OnPropertyChanged("StandbyFreq");
            }
        }

        public bool LedVhf1Set
        {
            get { return _LedVhf1Set; }
            private set
            {
                _LedVhf1Set = value;
                OnPropertyChanged("LedVhf1Set");
            }
        }

        public bool LedVhf2Set
        {
            get { return _LedVhf2Set; }
            private set
            {
                _LedVhf2Set = value;
                OnPropertyChanged("LedVhf2Set");
            }
        }

        public bool LedVhf3Set
        {
            get { return _LedVhf3Set; }
            private set
            {
                _LedVhf3Set = value;
                OnPropertyChanged("LedVhf3Set");
            }
        }

        public bool LedHf1Set
        {
            get { return _LedHf1Set; }
            private set
            {
                _LedHf1Set = value;
                OnPropertyChanged("LedHf1Set");
            }
        }

        public bool LedHf2Set
        {
            get { return _LedHf2Set; }
            private set
            {
                _LedHf2Set = value;
                OnPropertyChanged("LedHf2Set");
            }
        }

        public bool LedSelSet
        {
            get { return _LedSelSet; }
            private set
            {
                _LedSelSet = value;
                OnPropertyChanged("LedSelSet");
            }
        }

        public bool LedAmSet
        {
            get { return _LedAmSet; }
            private set
            {
                _LedAmSet = value;
                OnPropertyChanged("LedAmSet");
            }
        }

        public bool LedNavSet
        {
            get { return _LedNavSet; }
            private set
            {
                _LedNavSet = value;
                OnPropertyChanged("LedNavSet");
            }
        }

        public bool LedVorSet
        {
            get { return _LedVorSet; }
            private set
            {
                _LedVorSet = value;
                OnPropertyChanged("LedVorSet");
            }
        }

        public bool LedIlsSet
        {
            get { return _LedIlsSet; }
            private set
            {
                _LedIlsSet = value;
                OnPropertyChanged("LedIlsSet");
            }
        }

        public bool LedMlsSet
        {
            get { return _LedMlsSet; }
            private set
            {
                _LedMlsSet = value;
                OnPropertyChanged("LedMlsSet");
            }
        }

        public bool LedBfoSet
        {
            get { return _LedBfoSet; }
            private set
            {
                _LedBfoSet = value;
                OnPropertyChanged("LedBfoSet");
            }
        }

        public bool LedAdfSet
        {
            get { return _LedAdfSet; }
            private set
            {
                _LedAdfSet = value;
                OnPropertyChanged("LedAdfSet");
            }
        }

        private string _ActiveFreq;
        private string _StandbyFreq;
        private bool _LedVhf1Set;
        private bool _LedVhf2Set;
        private bool _LedVhf3Set;
        private bool _LedHf1Set;
        private bool _LedHf2Set;
        private bool _LedSelSet;
        private bool _LedAmSet;
        private bool _LedNavSet;
        private bool _LedVorSet;
        private bool _LedIlsSet;
        private bool _LedMlsSet;
        private bool _LedBfoSet;
        private bool _LedAdfSet;

        private SimData simdata;

        public RmpData()
        {
            simdata = SimData.GetInstance();
            simdata.DataUpdated += Simdata_DataUpdated;
            simdata.ConnectedToFs += Simdata_ConnectedToFs;
            InitializeVariables();
        }

        private void MainWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.MessageBox.Show(sender.ToString());
        }

        private void Simdata_ConnectedToFs(string FlightSimulatorName)
        {
            LedSelSet = false;
        }

        private void Simdata_DataUpdated(object sender, EventArgs e)
        {
            ActiveFreq = simdata.Com1ActiveFreq;
            StandbyFreq = simdata.Com1StandbyFreq;
            LedVhf1Set = true;
        }

        private void Test_DoWork(object sender, DoWorkEventArgs e)
        {
            LedSelSet = true;
            System.Threading.Thread.Sleep(2000);
            ActiveFreq = "122.800";
            StandbyFreq = "132.975";
            LedVhf1Set = true;
            LedSelSet = false;
        }

        private void InitializeVariables()
        {
            ActiveFreq = "---.---";
            StandbyFreq = "---.---";
            LedVhf1Set = false;
            LedSelSet = true;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
