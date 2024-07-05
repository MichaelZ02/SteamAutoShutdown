using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace SteamAutoShutdown
{
    /// <summary>
    /// Logica di interazione per MyToggleButton.xaml
    /// </summary>
    public partial class MyToggleButton : UserControl, INotifyPropertyChanged
    {
        public Brush ToggleBackground {  get; set; }

        public bool IsChecked { get => isChecked; set { value = isChecked; OnPropertyChanged(); } }

        private bool isChecked = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<RoutedEventArgs>? ToggleChanged;

        public MyToggleButton()
        {
            InitializeComponent();

            ToggleBackground = MainBorder.Background;
        }

        protected virtual void OnToggleChanged(RoutedEventArgs e)
        {
            ToggleChanged?.Invoke(this, e);
        }

        private void ToggleButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isChecked) 
            {
                isChecked = true;
                MainBorder.Background = Brushes.LimeGreen;
                ButtonStoryboard.To = new Thickness(ActualWidth - RoundButton.Width - 8, 0, 0, 0);

            }
            else
            {
                isChecked = false;
                MainBorder.Background= Brushes.IndianRed;
                ButtonStoryboard.To = new Thickness(2, 0, 0, 0);
            }

            ToggleBackground = MainBorder.Background;

            OnToggleChanged(e);
        }

        private void OnPropertyChanged()
        {
            throw new NotImplementedException();
        }
    }
}
