using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JeehellRMP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonTransfer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SimData.TransferKeyPressed();
        }

        private void Knob_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SimData.KnobDirection knobTurnDirection = e.LeftButton.HasFlag(MouseButtonState.Pressed) ? SimData.KnobDirection.CounterClockWise : SimData.KnobDirection.Clockwise;
            SimData.KnobTurned(DecideWhichKnob(sender as Ellipse), knobTurnDirection);
        }

        private void Knob_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            SimData.KnobDirection knobTurnDirection = (e.Delta < 0) ? SimData.KnobDirection.CounterClockWise : SimData.KnobDirection.Clockwise;
            SimData.KnobTurned(DecideWhichKnob(sender as Ellipse), knobTurnDirection);
        }

        private SimData.Knob DecideWhichKnob(Ellipse knob)
        {
            if (knob == null) return SimData.Knob.InnerKnob;

            return (knob.Name == "OuterKnob") ? SimData.Knob.OuterKnob : SimData.Knob.InnerKnob;
        }

        private void button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas buttonPressed = sender as Canvas;
            
            switch (buttonPressed.Name)
            {
                case "buttonVHF1":
                    RmpData.SetActiveMode(RmpData.RmpMode.VHF1);
                    break;
                case "buttonVHF2":
                    RmpData.SetActiveMode(RmpData.RmpMode.VHF2);
                    break;
            }

        }
    }
}