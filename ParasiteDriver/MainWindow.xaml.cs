using System;
using System.Windows;

namespace ParasiteDriver
{
    public partial class MainWindow : Window
    {
        private Driver _driver;
        private HotKey _hotKey;

        private SMB _smb;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hotKey = new HotKey(this);
            _hotKey.Pressed += hotKeyPressed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotKey.Cleanup();

            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _driver = new Driver();
            _smb = new SMB(_driver);
        }

        private void hotKeyPressed()
        {
            _driver.RequestScreen = true;
        }
    }
}
