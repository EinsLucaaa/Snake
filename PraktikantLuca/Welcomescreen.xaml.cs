using System.Windows;

namespace PraktikantLuca
{
    /// <summary>
    ///     Interaktionslogik für Welcomescreen.xaml
    /// </summary>
    public partial class Welcomescreen : Window
    {
        public Welcomescreen()
        {
            InitializeComponent();
        }


        private void BootScreen_Click(object o, RoutedEventArgs e)
        {
            Hide();

            var main = new MainWindow();
            main.Show();
        }


        private void btnGameStart_Click(object o, RoutedEventArgs e)
        {
            Hide();


            var main = new MainWindow();
            main.Show();
        }
    }
}