using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Store.Context;
using Store.Entities;

namespace Store;

public partial class RegisterControl : UserControl
{
    private LoginControl? _loginControl;
    public RegisterControl()
    {
        InitializeComponent();
    }

    private void ShowLogin()
    {
        if (Parent is TransitioningContentControl mainContentControl)
        {
            _loginControl ??= new LoginControl();
            mainContentControl.Content = _loginControl;
        }
    }
    
    private void ViewLogin_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowLogin();
    }

    private async void Registration_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string lastName  = LastNameInput.Text?.Trim() ?? string.Empty;
            string firstName = FirstNameInput.Text?.Trim() ?? string.Empty;
            string email     = EmailInput.Text?.Trim() ?? string.Empty;
            string password  = PasswordInput.Text?.Trim() ?? string.Empty;
            string phone     = PhoneInput.Text?.Trim() ?? string.Empty;
            string countryCode = CountryCodeBox.Text?.Trim() ?? string.Empty;
        
            var errors = new List<string>();
        
            if (string.IsNullOrWhiteSpace(lastName))
            {
                errors.Add("• Поле 'Фамилия' не может быть пустым.");
            }
        
            if (string.IsNullOrWhiteSpace(firstName))
            {
                errors.Add("• Поле 'Имя' не может быть пустым.");
            }
        
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("• Поле 'Email' не может быть пустым.");
            }
            else if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                errors.Add("• Некорректный формат Email.");
            }
        
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("• Поле 'Пароль' не может быть пустым.");
            }
            else if (password.Length < 8)
            {
                errors.Add("• Пароль должен быть не менее 8 символов.");
            }
        
            if (string.IsNullOrWhiteSpace(phone))
            {
                errors.Add("• Поле 'Телефон' не может быть пустым.");
            }
            else if (!Regex.IsMatch(phone, @"^[0-9]{10}$"))
            {
                errors.Add("• Номер телефона должен состоять ровно из 10 цифр.");
            }
        
            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);
                
                NotificationService.Notify("Ошибка заполнения формы", errorMessage, NotificationType.Error);
                return;
            }
        
            string fullPhoneNumber = countryCode + phone;
            string fullName = $"{firstName} {lastName}";
            string hashPassword = BCrypt.Net.BCrypt.HashPassword(password);

            Client newClient = new Client
            {
                FullName = fullName,
                Email = email,
                Password = hashPassword,
                PhoneNumber = fullPhoneNumber,
            };

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                db.Clients.Add(newClient);
                await db.SaveChangesAsync().ConfigureAwait(false);
            });

            NotificationService.Notify("Успешная регистрация!", "Аккаунт успешно зарегистрирован!", NotificationType.Success);
            
            ShowLogin();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!",$"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}",  NotificationType.Error);
        }
    }
}