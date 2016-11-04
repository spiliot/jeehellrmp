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
        static int WindowRotationAngle = 0;

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
            if ((WindowRotationAngle -= 90) < 0) WindowRotationAngle += 360;
            RotateMainWindow(WindowRotationAngle);
        }

        private void RotateMainWindowCW()
        {
            if ((WindowRotationAngle += 90) > 359) WindowRotationAngle -= 360;
            RotateMainWindow(WindowRotationAngle);
        }

        private void RotateMainWindow(int Angle)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            RotateTransform myRotateTransform = new RotateTransform(WindowRotationAngle);

            mainWindow.ContainerViewbox.LayoutTransform = myRotateTransform;

            var titleHeight = SystemParameters.WindowCaptionHeight + SystemParameters.ResizeFrameHorizontalBorderHeight;
            var horizontalBorderHeight = SystemParameters.ResizeFrameHorizontalBorderHeight;
            var verticalBorderWidth = SystemParameters.ResizeFrameVerticalBorderWidth;

            double currentInternaWindowlHeight = mainWindow.Height - titleHeight - horizontalBorderHeight;
            double currentInternaWindowlWidth = mainWindow.Width - 2 * verticalBorderWidth;
            mainWindow.Height = currentInternaWindowlWidth + titleHeight + horizontalBorderHeight;
            mainWindow.Width = currentInternaWindowlHeight + 2 * verticalBorderWidth;
        }

        private void MenuItem_Proportions_Click(object sender, RoutedEventArgs e)
        {
            MenuItem proportionItem = sender as MenuItem;
            Viewbox container = Application.Current.MainWindow.FindName("ContainerViewbox") as Viewbox;

            if (proportionItem.IsChecked)
            {
                container.Stretch = Stretch.Uniform;
                return;
            }
            container.Stretch = Stretch.Fill;
        }

        private void MenuItem_JhColors_Click(object sender, RoutedEventArgs e)
        {
            MenuItem JhColorsItem = sender as MenuItem;
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (JhColorsItem.IsChecked)
            {
                mainWindow.Background = new SolidColorBrush(new Color() { A = 0xFF, R = 0x8D, G = 0xAE, B = 0xBD });
                return;
            }
            mainWindow.Background = new SolidColorBrush(new Color() { A = 0xFF, R = 0x57, G = 0x69, B = 0x75 });
        }

        private void MenuItem_OnTop_Click(object sender, RoutedEventArgs e)
        {
            MenuItem onTopItem = sender as MenuItem;
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            mainWindow.Topmost = onTopItem.IsChecked;
        }
    }
}