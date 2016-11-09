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

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // We don't want to do anything if right click was on a knob
            FrameworkElement element = e.OriginalSource as FrameworkElement;
            if (element.Name.EndsWith("Knob")) return;

            MainWindow mainWindow = sender as MainWindow;
            ContextMenu menu = this.FindResource("RightClickMenu") as ContextMenu;
            menu.IsOpen = true;
        }

        private void MenuItem_Rotate_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            switch (item.Tag as string)
            {
                case "CW":
                    RotateMainWindowCW();
                    break;
                case "CCW":
                    RotateMainWindowCCW();
                    break;
            }
        }

        private void RotateMainWindowCCW()
        {
            int WindowRotationAngle = Properties.Settings.Default.WindowRotationAngle;
            if ((WindowRotationAngle -= 90) < 0) WindowRotationAngle += 360;
            Properties.Settings.Default.WindowRotationAngle = WindowRotationAngle;
            RotateMainWindow();
        }

        private void RotateMainWindowCW()
        {
            int WindowRotationAngle = Properties.Settings.Default.WindowRotationAngle;
            if ((WindowRotationAngle += 90) > 359) WindowRotationAngle -= 360;
            Properties.Settings.Default.WindowRotationAngle = WindowRotationAngle;
            RotateMainWindow();
        }

        private void RotateMainWindow()
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            var titleHeight = SystemParameters.WindowCaptionHeight + SystemParameters.ResizeFrameHorizontalBorderHeight;
            var horizontalBorderHeight = SystemParameters.ResizeFrameHorizontalBorderHeight;
            var verticalBorderWidth = SystemParameters.ResizeFrameVerticalBorderWidth;

            double currentInternaWindowlHeight = mainWindow.Height - titleHeight - horizontalBorderHeight;
            double currentInternaWindowlWidth = mainWindow.Width - 2 * verticalBorderWidth;
            mainWindow.Height = currentInternaWindowlWidth + titleHeight + horizontalBorderHeight;
            mainWindow.Width = currentInternaWindowlHeight + 2 * verticalBorderWidth;
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetWindowPositionIfOutOfView();
        }

        private void ResetWindowPositionIfOutOfView()
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (mainWindow.Top + mainWindow.Height / 2 < SystemParameters.VirtualScreenHeight) return;
            if (mainWindow.Left + mainWindow.Width / 2 > SystemParameters.VirtualScreenWidth) return;
            mainWindow.Top = 0;
            mainWindow.Left = 0;
        }
    }
}