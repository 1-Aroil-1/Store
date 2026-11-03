using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace Store;

public static class NotificationService
{
    private static WindowNotificationManager? _manager;
    
    public static void Initialize(TopLevel topLevel)
    {
        _manager = new WindowNotificationManager(topLevel)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 5
        };
    }
    
    public static void Notify(string title, string message, NotificationType type = NotificationType.Information, int seconds = 4)
    {
        if (_manager == null) return;
        
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _manager.Show(new Notification(title, message, type, TimeSpan.FromSeconds(seconds)));
        });
    }
}