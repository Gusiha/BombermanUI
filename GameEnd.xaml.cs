using System;
using System.Collections.Generic;
using System.Linq;
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
using WpfApp1;

namespace BomberGameUI
{
    /// <summary>
    /// Interaction logic for GameEnd.xaml
    /// </summary>
    public partial class GameEnd : Page
    {
        public GameEnd(string status)
        {
            InitializeComponent();
            PlayerStatus.Content = status;
        }

        private void ToMenu_Click(object sender, RoutedEventArgs e)
        {
            WpfApp1.Menu menu = new();

            this.NavigationService.Navigate(menu);
        }
    }
}
