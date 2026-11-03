using Avalonia.Controls;

namespace Store;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainContentControl.Content = new LoginControl();
        NotificationService.Initialize(this);
    }
}