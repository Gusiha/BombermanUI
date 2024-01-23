using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
    public partial class Menu : Page
    {
        public Menu()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IPAddress.Parse(ServerIP.Text);
                Int32.Parse(Port.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            Game game = new (ServerIP.Text, Port.Text);
            this.NavigationService.Navigate(game);
        }
    }
}
