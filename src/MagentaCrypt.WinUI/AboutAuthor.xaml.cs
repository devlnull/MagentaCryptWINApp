using System.Diagnostics;
using System.Windows;

namespace WinUI
{
    /// <summary>
    /// This page is about author.
    /// </summary>
    public partial class AboutAuthor : Window
    {
        public AboutAuthor()
        {
            InitializeComponent();
        }

        private void frmAbout_Loaded(object sender, RoutedEventArgs e)
        {
            OpenWebPage();
        }

        private void OpenWebPage()
        {
            try
            {
                Process.Start("http://www.amirhosseinghorbani.com");
            }
            catch { }
        }

        private void img_about_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                OpenWebPage();
        }
    }
}
