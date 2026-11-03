using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using Store.Context;
using Store.Entities;

namespace Store;

public partial class LoginControl : UserControl
{
    private RegisterControl? _registerControl;
    private UsersControl? _usersControl;
    private AdminControl? _adminControl;
    public LoginControl()
    {
        InitializeComponent();
    }

    private void ViewRegistration_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Parent is TransitioningContentControl mainContentControl)
        {
            _registerControl ??= new RegisterControl();
            mainContentControl.Content = _registerControl;
        }
    }

    private void ShowUser(Client client)
    {
        if (Parent is TransitioningContentControl mainContentControl)
        {
            _usersControl ??= new UsersControl(client);
            mainContentControl.Content = _usersControl;
        }
    }

    private void ShowAdmin(Client client)
    {
        if (Parent is TransitioningContentControl mainContentControl)
        {
            _adminControl ??= new AdminControl(client);
            mainContentControl.Content = _adminControl;
        }
    }

    private async void Login_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string email     = EmailInput.Text?.Trim() ?? string.Empty;
            string password  = PasswordInput.Text?.Trim() ?? string.Empty;
        
            bool isEmailValid = !string.IsNullOrWhiteSpace(email);
            bool isPasswordValid = !string.IsNullOrWhiteSpace(password);

            var errors = new List<string>();

            if (!isEmailValid)
            {
                errors.Add("• Поле 'Email' не может быть пустым.");
            }

            if (!isPasswordValid)
            {
                errors.Add("• Поле 'Пароль' не может быть пустым.");
            }
            
            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);

                NotificationService.Notify("Ошибка заполнения формы", errorMessage,  NotificationType.Error);
                return;
            }
            
            Client? client = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                return await db.Clients.FirstOrDefaultAsync(c => c.Email == email);
            });
            
            if (client == null || !BCrypt.Net.BCrypt.Verify(password, client.Password))
            {
                NotificationService.Notify("Ошибка входа", "Неверный логин или пароль!", NotificationType.Error);
                return;
            }

            NotificationService.Notify("Успешно!", "Выполнен вход в аккаунт!", NotificationType.Success);
            
            if (client.Role == 1)
            {
                ShowAdmin(client);
            } else if (client.Role == 2)
            {
                ShowUser(client);
            }
            else
            {
                NotificationService.Notify("Ошибка!","Произошла непредвиденная ошибка! (Unknown role)", NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!",$"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}", NotificationType.Error);
        }
    }
}