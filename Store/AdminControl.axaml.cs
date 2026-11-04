using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClosedXML.Excel;
using Material3.Avalonia.Controls;
using Microsoft.EntityFrameworkCore;
using Store.Context;
using Store.Entities;

namespace Store;

public partial class AdminControl : UserControl
{
    private ObservableCollection<CartItem> Cart { get; } = [];
    private readonly Client _client;
    private LoginControl? _loginControl;
    public AdminControl(Client client)
    {
        _client = client;
        InitializeComponent();
        LoadData();
    }
    
    /*
     * ==================================================================
     *                          Привязка индекса с Листа на Карусель
     * ==================================================================
     */
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MainMenu == null || ContentCarousel == null) return;
        ContentCarousel.SelectedIndex = MainMenu.SelectedIndex;
    }
    
    /*
     * ==================================================================
     *                          Подгрузка данных с БД'шечки
     * ==================================================================
     */
    private async void LoadClientsData()
    {
        try
        {
            var data = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                var list = await db.Clients.ToListAsync();
                var currentClient = list.FirstOrDefault(c => c.Id == _client.Id);
                return new{
                    Clients = list,
                    CurrentClient = currentClient
                };
            });
            
            ClientsTableView.ItemsSource = data.Clients;
            
            AccountFullName.Text = data.CurrentClient?.FullName;
            AccountEmail.Text = data.CurrentClient?.Email;
            AccountPhone.Text = data.CurrentClient?.PhoneNumber;
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!",$"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}",  NotificationType.Error);
        }
    }
    
    private async void LoadProductsData()
    {
        try
        {
            var data = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                var list = await db.Products.ToListAsync();
                
                decimal minPrice = list.Count > 0 ? list.Min(p => p.Price) : 0;
                decimal maxPrice = list.Count > 0 ? list.Max(p => p.Price) : 100000;

                return new
                {
                    Products = list,
                    MinPriceDouble = (double)minPrice,
                    MaxPriceDouble = (double)maxPrice
                };
            });
            
            SearchProductPriceRangeSlider.Minimum = data.MinPriceDouble;
            SearchProductPriceRangeSlider.Maximum = data.MaxPriceDouble;
            SearchProductPriceRangeSlider.LowerValue = data.MinPriceDouble;
            SearchProductPriceRangeSlider.UpperValue = data.MaxPriceDouble;
        
            ProductsTableView.ItemsSource = data.Products;
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!", $"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}", NotificationType.Error);
        }
    }


    private async void LoadOrdersData()
    {
        try
        {
            var ordersList = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                return await db.Orders.Include(o => o.Client).ToListAsync();
            });
            
            OrdersTableView.ItemsSource = ordersList;
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!",$"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}",  NotificationType.Error);
        }
    }

    private async void LoadCartData()
    {
        try
        {
            var data = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                return new
                {
                    ClientsList = await db.Clients.Where(c => c.Role == 2).ToListAsync(),
                    ProductList = await db.Products.Where(p => p.StockQuantity > 0).ToListAsync()
                };

            });
            
            OrderClientComboBox.ItemsSource = data.ClientsList;
            OrderProductComboBox.ItemsSource = data.ProductList;
            CartListBox.ItemsSource = Cart;
            
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!",$"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}",  NotificationType.Error);
        }
    }

    private void LoadData()
    {
        LoadClientsData();
        LoadProductsData();
        LoadOrdersData();
        LoadCartData();
    }

    /*
     * ==================================================================
     *                          Кнопочки Личного кабинета
     * ==================================================================
     */
    private void ChangePassword_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var rootContainer = OverlayLayer.GetOverlayLayer(topLevel);
            
            if (rootContainer == null)
            {
                throw new Exception("Не удалось найти корневой контейнер для отображения диалога.");
            }
            
            var newPassword = new TextBox { PlaceholderText = "********", PasswordChar = '*' };
            var changePass = new Button { Content = "Сменить пароль" };
            var closeBtn = new Button { Content = "Отмена" };
            
            var dialogOverlay = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                Width = topLevel.Bounds.Width,
                Height = topLevel.Bounds.Height,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
                
            topLevel.PropertyChanged += (_, ev) =>
            {
                if (ev.Property == BoundsProperty)
                {
                    dialogOverlay.Width = topLevel.Bounds.Width;
                    dialogOverlay.Height = topLevel.Bounds.Height;
                }
            };
            
            var dialogContent = new Card
            {
                CornerRadius = new CornerRadius(28),
                Padding = new Thickness(24),
                Variant = CardVariant.Outlined,
                Width = 500,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = new StackPanel
                {
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock { Text = "Смена пароля", FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 16 },
                        newPassword,
                        changePass,
                        closeBtn
                    }
                }
            };

            dialogOverlay.Children.Add(dialogContent);
            
            closeBtn.Click += (_, _) => rootContainer.Children.Remove(dialogOverlay);
            
            changePass.Click += async (_, _) =>
            {
                string? passwordText = newPassword.Text;

                if (string.IsNullOrWhiteSpace(passwordText) || passwordText.Length < 8)
                {
                    NotificationService.Notify("Внимание", "Пароль не может быть пустым или менее 8 символов", NotificationType.Warning);
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(passwordText);

                await Task.Run(async () =>
                {
                    await using var db = new StoreDB();
                    var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == _client.Id);
                    if (client != null)
                    {
                        client.Password = hashedPassword;
                        db.Clients.Update(client);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                });

                NotificationService.Notify("Успех!", "Пароль успешно изменен.", NotificationType.Success);
                
                rootContainer.Children.Remove(dialogOverlay);
            };
            
            rootContainer.Children.Add(dialogOverlay);
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка", ex.Message, NotificationType.Error);
        }
    }


    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Parent is TransitioningContentControl mainContentControl)
        {
            _loginControl ??= new LoginControl();
            mainContentControl.Content = _loginControl;
        }
    }

    /*
     * ==================================================================
     *                          Детали заказа
     * ==================================================================
     */
    private async void OnOrderSelected(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (OrdersTableView.SelectedItem is Order selectedOrder)
            {
                var details = await Task.Run(async () =>
                {
                    await using var db = new StoreDB();
                    return await db.OrderDetails
                        .Include(od => od.Product)
                        .Where(od => od.OrderId == selectedOrder.Id)
                        .ToListAsync();
                });

                OrderDetailsTableView.ItemsSource = details;
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка", $"Не удалось загрузить состав заказа: {ex.Message}", NotificationType.Error);
        }
    }
    
    /*
     * ==================================================================
     *                          История заказов
     * ==================================================================
     */
    private async void ClientsTableView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ClientsTableView.SelectedItem is Client selectedClient)
            {
                ClientRoleComboBox.SelectedIndex = selectedClient.Role == 1 ? 0 : 1;
                var ordersHistory = await Task.Run(async () =>
                {
                    await using var db = new StoreDB();
                    
                    return await db.Orders
                        .Where(o => o.ClientId == selectedClient.Id)
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync()
                        .ConfigureAwait(false);
                });
                
                ClientOrdersHistoryTableView.ItemsSource = ordersHistory;
            }
            else
            {
                ClientRoleComboBox.SelectedIndex = 1;
                ClientOrdersHistoryTableView.ItemsSource = null;
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка истории", $"Не удалось загрузить историю заказов: {ex.Message}", NotificationType.Error);
        }
    }
    
    /*
     * ==================================================================
     *                          Фильтрация && сортировка товаров
     * ==================================================================
     */

    private void OnResetProductSearchClick(object? sender, RoutedEventArgs e)
    {
        SearchProductNameTextBox.Text = string.Empty;
        SearchProductCategoryTextBox.Text = string.Empty;
        
        SearchProductSortComboBox.SelectedIndex = 0;
        
        if (SearchProductPriceRangeSlider != null)
        {
            SearchProductPriceRangeSlider.LowerValue = SearchProductPriceRangeSlider.Minimum;
            SearchProductPriceRangeSlider.UpperValue = SearchProductPriceRangeSlider.Maximum;
        }
        
        ApplyFilterAsyncProduct();
    }
    
    private void OnProductSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilterAsyncProduct();
    }
    
    private void OnProductSearchSliderChanged(object? sender, RangeSliderValueChangedEventArgs e)
    {
        ApplyFilterAsyncProduct();
    }
    
    private void OnProductSortChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyFilterAsyncProduct();
    }
    
    private async void ApplyFilterAsyncProduct()
    {
        try
        {
            if (SearchProductNameTextBox == null || 
                SearchProductCategoryTextBox == null || 
                SearchProductPriceRangeSlider == null || 
                SearchProductSortComboBox == null)
            {
                return;
            }
            
            string nameText = SearchProductNameTextBox.Text?.Trim() ?? string.Empty;
            string categoryText = SearchProductCategoryTextBox.Text?.Trim() ?? string.Empty;
            
            decimal minPrice = (decimal)SearchProductPriceRangeSlider.LowerValue;
            decimal maxPrice = (decimal)SearchProductPriceRangeSlider.UpperValue;
            
            int sortIndex = SearchProductSortComboBox.SelectedIndex;
            
            var filteredList = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                
                var query = db.Products.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(nameText))
                {
                    query = query.Where(p => p.Name.Contains(nameText));
                }
                
                if (!string.IsNullOrWhiteSpace(categoryText))
                {
                    query = query.Where(p => p.Category != null && p.Category.Contains(categoryText));
                }
                
                if (maxPrice > 0) 
                {
                    query = query.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
                }
                
                query = sortIndex switch
                {
                    0 => query.OrderByDescending(p => p.Price),
                    1 => query.OrderBy(p => p.Price),
                    2 => query.OrderByDescending(p => p.StockQuantity),
                    3 => query.OrderBy(p => p.StockQuantity),
                    _ => query.OrderBy(p => p.Id)
                };
                
                return await query.ToListAsync().ConfigureAwait(false);
            });
            
            ProductsTableView.ItemsSource = filteredList;
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка поиска", $"Не удалось отфильтровать каталог товаров: {ex.Message}", NotificationType.Error
            );
        }
    }

    
    /*
     * ==================================================================
     *                          Фильтрация заказов
     * ==================================================================
     */
    private void OnSearchTextChangedOrders(object? sender, TextChangedEventArgs e)
    {
        ApplyFilterAsyncOrders();
    }
    
    private void OnSearchDateChangedOrders(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        ApplyFilterAsyncOrders();
    }

    private void OnResetSearchClickOrders(object? sender, RoutedEventArgs e)
    {
        SearchClientTextBox.Text = string.Empty;
        SearchDatePicker.SelectedDate = null;
        ApplyFilterAsyncOrders(); 
    }

    private async void ApplyFilterAsyncOrders()
    {
        try
        {
            string searchText = SearchClientTextBox.Text?.Trim() ?? string.Empty;
            DateTime? targetDate = SearchDatePicker.SelectedDate?.DateTime.Date;

            var filteredOrders = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                var query = db.Orders.Include(o => o.Client).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(o => o.Client.FullName.Contains(searchText));
                }

                if (targetDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate.Date == targetDate.Value);
                }

                return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            });

            OrdersTableView.ItemsSource = filteredOrders;
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка поиска", $"Не удалось отфильтровать заказы: {ex.Message}", NotificationType.Error);
        }
    }
    
    /*
     * ==================================================================
     *                          CRUD операции с клиентами
     * ==================================================================
     */
    private async void OnUpdateClientClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ClientsTableView.SelectedItem is not Client selectedClient) return;

            string fullName = ClientFullNameTextBox.Text?.Trim() ?? string.Empty;
            string email     = ClientEmailTextBox.Text?.Trim() ?? string.Empty;
            string password  = ClientPasswordTextBox.Text?.Trim() ?? string.Empty;
            string phone     = ClientPhoneTextBox.Text?.Trim() ?? string.Empty;
            
            var errors = new List<string>();
        
            if (string.IsNullOrWhiteSpace(fullName))
            {
                errors.Add("• Поле 'Полное имя' не может быть пустым.");
            }
        
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("• Поле 'Email' не может быть пустым.");
            }
            else if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                errors.Add("• Некорректный формат Email.");
            }
            
            if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
            {
                errors.Add("• Пароль должен быть не менее 8 символов.");
            }
        
            if (string.IsNullOrWhiteSpace(phone))
            {
                errors.Add("• Поле 'Телефон' не может быть пустым.");
            }
            else if (!Regex.IsMatch(phone, @"^[+]+[0-9]{11}$"))
            {
                errors.Add("• Номер телефона должен состоять ровно из 10 цифр.");
            }
        
            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);
                NotificationService.Notify("Ошибка заполнения формы", errorMessage, NotificationType.Error);
                return;
            }
        
            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                var dbClient = await db.Clients.FirstOrDefaultAsync(c => c.Id == selectedClient.Id);
            
                if (dbClient != null)
                {
                    dbClient.FullName = fullName;
                    dbClient.Email = email;
                    dbClient.PhoneNumber = phone;
                    dbClient.Role = ClientRoleComboBox.SelectedIndex == 0 ? 1 : 2;

                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        dbClient.Password = BCrypt.Net.BCrypt.HashPassword(password);
                    }

                    db.Clients.Update(dbClient);
                    await db.SaveChangesAsync();
                }
            });

            NotificationService.Notify("Успех!", "Данные клиента обновлены.", NotificationType.Success);
            ClientPasswordTextBox.Text = string.Empty;
            LoadClientsData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка", ex.Message, NotificationType.Error);
        }
    }

    
    private async void OnCreateClientClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string fullName = ClientFullNameTextBox.Text?.Trim() ?? string.Empty;
            string email     = ClientEmailTextBox.Text?.Trim() ?? string.Empty;
            string password  = ClientPasswordTextBox.Text?.Trim() ?? string.Empty;
            string phone     = ClientPhoneTextBox.Text?.Trim() ?? string.Empty;
        
            var errors = new List<string>();
        
            if (string.IsNullOrWhiteSpace(fullName))
            {
                errors.Add("• Поле 'Полное имя' не может быть пустым.");
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
            else if (!Regex.IsMatch(phone, @"^[+]+[0-9]{11}$"))
            {
                errors.Add("• Номер телефона должен состоять ровно из 11 цифр и знака + в начале.");
            }
        
            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);
                NotificationService.Notify("Ошибка заполнения формы", errorMessage, NotificationType.Error);
                return;
            }
            

            var newClient = new Client
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = ClientRoleComboBox.SelectedIndex == 0 ? 1 : 2 
            };

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                db.Clients.Add(newClient);
                await db.SaveChangesAsync();
            });

            NotificationService.Notify("Успех!", "Новый client успешно добавлен.", NotificationType.Success);
        
            ClientFullNameTextBox.Text = ClientEmailTextBox.Text = ClientPhoneTextBox.Text = ClientPasswordTextBox.Text = string.Empty;
        
            LoadClientsData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка создания", ex.Message, NotificationType.Error);
        }
    }

    private async void OnDeleteClientClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ClientsTableView.SelectedItem is not Client selectedClient) return;
        
            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                
                bool hasOrders = await db.Orders.AnyAsync(o => o.ClientId == selectedClient.Id);
                if (hasOrders)
                {
                    throw new Exception("Нельзя удалить клиента, у которого есть оформленные заказы в истории!");
                }

                var dbClient = await db.Clients.FirstOrDefaultAsync(c => c.Id == selectedClient.Id);
                if (dbClient != null)
                {
                    db.Clients.Remove(dbClient);
                    await db.SaveChangesAsync();
                }
            });

            NotificationService.Notify("Успех!", "Клиент полностью удален из базы.", NotificationType.Success);
            LoadClientsData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка удаления", ex.Message, NotificationType.Error);
        }
    }
    
    /*
     * ==================================================================
     *                          CRUD операции с Товарами
     * ==================================================================
     */
    private async void OnUpdateProductClick(object? sender, RoutedEventArgs e)
    {
        try
        { 
            if (ProductsTableView.SelectedItem is not Product selectedProduct) return;
            
            string name = ProductNameTextBox.Text?.Trim() ?? string.Empty;
            string category = ProductCategoryTextBox.Text?.Trim() ?? string.Empty;
            string priceRaw = ProductPriceTextBox.Text?.Trim() ?? string.Empty;
            string stockRaw = ProductStockQuantityTextBox.Text?.Trim() ?? string.Empty;

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("• Поле 'Название товара' не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                errors.Add("• Поле 'Категория товара' не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(priceRaw))
            {
                errors.Add("• Поле 'Цена' не может быть пустым.");
            }
            else if (!decimal.TryParse(priceRaw, out decimal p) || p < 0)
            {
                errors.Add("• Цена должна быть положительным числом.");
            }

            if (string.IsNullOrWhiteSpace(stockRaw))
            {
                errors.Add("• Поле 'Кол-во на складе' не может быть пустым.");
            }
            else if (!int.TryParse(stockRaw, out int s) || s < 0)
            {
                errors.Add("• Количество должно быть целым положительным числом.");
            }

            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);
                NotificationService.Notify("Ошибка заполнения формы", errorMessage, NotificationType.Error);
                return;
            }

            decimal price = decimal.Parse(priceRaw);
            int stock = int.Parse(stockRaw);

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                var dbProduct = await db.Products.FirstOrDefaultAsync(p => p.Id == selectedProduct.Id);
            
                if (dbProduct != null)
                {
                    dbProduct.Name = name;
                    dbProduct.Category = category;
                    dbProduct.Price = price;
                    dbProduct.StockQuantity = stock;

                    db.Products.Update(dbProduct);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            });

            NotificationService.Notify("Успех!", "Товар успешно обновлен.", NotificationType.Success);
            LoadProductsData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка", ex.Message, NotificationType.Error);
        }
    }

    private async void OnCreateProductClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string name = ProductNameTextBox.Text?.Trim() ?? string.Empty;
            string category = ProductCategoryTextBox.Text?.Trim() ?? string.Empty;
            string priceRaw = ProductPriceTextBox.Text?.Trim() ?? string.Empty;
            string stockRaw = ProductStockQuantityTextBox.Text?.Trim() ?? string.Empty;

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("• Поле 'Название товара' не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                errors.Add("• Поле 'Категория товара' не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(priceRaw))
            {
                errors.Add("• Поле 'Цена' не может быть пустым.");
            }
            else if (!decimal.TryParse(priceRaw, out decimal p) || p < 0)
            {
                errors.Add("• Цена должна быть положительным числом.");
            }

            if (string.IsNullOrWhiteSpace(stockRaw))
            {
                errors.Add("• Поле 'Кол-во на складе' не может быть пустым.");
            }
            else if (!int.TryParse(stockRaw, out int s) || s < 0)
            {
                errors.Add("• Количество должно быть целым положительным числом.");
            }

            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);
                NotificationService.Notify("Ошибка заполнения формы", errorMessage, NotificationType.Error);
                return;
            }
            
            decimal price = decimal.Parse(priceRaw);
            int stock = int.Parse(stockRaw);

            var newProduct = new Product
            {
                Name = name,
                Category = category,
                Price = price,
                StockQuantity = stock
            };

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                db.Products.Add(newProduct);
                await db.SaveChangesAsync().ConfigureAwait(false);
            });

            NotificationService.Notify("Успех!", "Новый товар добавлен в каталог.", NotificationType.Success);
            ProductNameTextBox.Text = ProductCategoryTextBox.Text = ProductPriceTextBox.Text = ProductStockQuantityTextBox.Text = string.Empty;
            LoadProductsData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка", ex.Message, NotificationType.Error);
        }
    }

    private async void OnDeleteProductClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ProductsTableView.SelectedItem is not Product selectedProduct) return;

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                
                bool isInOrders = await db.OrderDetails.AnyAsync(od => od.ProductId == selectedProduct.Id);
                if (isInOrders)
                {
                    throw new Exception("Нельзя удалить товар, который уже фигурирует в оформленных заказах!");
                }

                var dbProduct = await db.Products.FirstOrDefaultAsync(p => p.Id == selectedProduct.Id);
                if (dbProduct != null)
                {
                    db.Products.Remove(dbProduct);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            });

            NotificationService.Notify("Успех!", "Товар удален из каталога.", NotificationType.Success);
            LoadProductsData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка", ex.Message, NotificationType.Error);
        }
    }
    
    /*
     * ==================================================================
     *                          Создание заказа || "Корзина"
     * ==================================================================
     */
    private void OnAddToBagClick(object? sender, RoutedEventArgs e)
    {
        if (OrderProductComboBox.SelectedItem is not Product selectedProduct)
        {
            NotificationService.Notify("Внимание", "Выберите товар для добавления!", NotificationType.Warning);
            return;
        }

        int quantity = (int)(OrderQuantitySpinner.Value ?? 1);

        if (quantity > selectedProduct.StockQuantity)
        {
            NotificationService.Notify("Ошибка", $"На складе доступно только {selectedProduct.StockQuantity} шт. этого товара.", NotificationType.Error);
            return;
        }
        
        var existingItem = Cart.FirstOrDefault(i => i.Product.Id == selectedProduct.Id);
        if (existingItem != null)
        {
            if (existingItem.Quantity + quantity > selectedProduct.StockQuantity)
            {
                NotificationService.Notify("Ошибка", "Суммарное количество в корзине превышает остаток на складе!", NotificationType.Error);
                return;
            }
            existingItem.Quantity += quantity;
            
            int index = Cart.IndexOf(existingItem);
            Cart[index] = existingItem;
        }
        else
        {
            Cart.Add(new CartItem { Product = selectedProduct, Quantity = quantity });
        }
        
        TotalOrderAmountTextBlock.Text = $"{Cart.Sum(item => item.Total)} руб.";
    }
    
    private async void OnCheckoutClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (OrderClientComboBox.SelectedItem is not Client selectedClient)
            {
                NotificationService.Notify("Внимание", "Пожалуйста, выберите клиента для оформления заказа!", NotificationType.Warning);
                return;
            }

            if (Cart.Count == 0)
            {
                NotificationService.Notify("Внимание", "Корзина пуста. Нечего оформлять.", NotificationType.Warning);
                return;
            }

        
            int clientId = selectedClient.Id;
            decimal totalAmount = Cart.Sum(item => item.Total);

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                
                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    var newOrder = new Order
                    {
                        ClientId = clientId,
                        OrderDate = DateTime.Now,
                        TotalAmount = totalAmount
                    };
                    db.Orders.Add(newOrder);
                    await db.SaveChangesAsync();
                    
                    foreach (var cartItem in Cart)
                    {
                        var dbProduct = await db.Products.FirstOrDefaultAsync(p => p.Id == cartItem.Product.Id);
                        
                        if (dbProduct == null) 
                            throw new Exception($"Товар '{cartItem.Product.Name}' удален из базы данных.");
                        
                        if (dbProduct.StockQuantity < cartItem.Quantity)
                            throw new Exception($"Критическая ошибка! Товар '{dbProduct.Name}' успели раскупить. На складе осталось: {dbProduct.StockQuantity} шт.");
                        
                        dbProduct.StockQuantity -= cartItem.Quantity;
                        db.Products.Update(dbProduct);
                        
                        var orderDetail = new OrderDetail
                        {
                            OrderId = newOrder.Id,
                            ProductId = cartItem.Product.Id,
                            Quantity = cartItem.Quantity,
                            PricePerUnit = cartItem.PricePerUnit
                        };
                        db.OrderDetails.Add(orderDetail);
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            NotificationService.Notify("Успех!", "Заказ успешно зарегистрирован, товары списаны со склада.", NotificationType.Success);
            
            Cart.Clear();
            TotalOrderAmountTextBlock.Text = "0 руб.";
            OrderProductComboBox.SelectedItem = null;
            OrderQuantitySpinner.Value = 1;
            
            LoadData();
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка оформления", ex.Message, NotificationType.Error);
        }
    }
    
    private void OnRemoveFromCartClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CartItem itemToRemove })
        {
            Cart.Remove(itemToRemove);
            TotalOrderAmountTextBlock.Text = $"{Cart.Sum(item => item.Total)} руб.";
            NotificationService.Notify("Корзина", $"Товар '{itemToRemove.Product.Name}' удален из чека.", NotificationType.Warning);
        }
    }
    
    private async void OnGenerateReportClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            DateTime? startDate = ReportStartDatePicker.SelectedDate?.DateTime.Date;
            DateTime? endDate = ReportEndDatePicker.SelectedDate?.DateTime.Date;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                NotificationService.Notify("Внимание", "Укажите начальную и конечную даты для отчета!", NotificationType.Warning);
                return;
            }

            if (startDate > endDate)
            {
                NotificationService.Notify("Ошибка", "Дата 'С' не может быть больше даты 'По'!", NotificationType.Error);
                return;
            }
        
            DateTime endDateTime = endDate.Value.AddDays(1);
            
            var reportData = await Task.Run(async () =>
            {
                await using var db = new StoreDB();

                return await db.Orders
                    .Include(o => o.Client)
                    .Where(o => o.OrderDate >= startDate.Value && o.OrderDate < endDateTime)
                    .GroupBy(o => o.ClientId)
                    .Select(group => new TopClientReportItem
                    {
                        FullName = group.First().Client.FullName,
                        OrdersCount = group.Count(),
                        TotalSpent = group.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(item => item.TotalSpent)
                    .Take(5)
                    .ToListAsync().ConfigureAwait(false);
            });
            
            if (reportData.Count == 0)
            {
                NotificationService.Notify("Информация", "За выбранный период заказов не найдено.", NotificationType.Warning);
                return;
            }
            
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                $"Отчет_Топ_Клиентов_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Топ Клиентов");
                
                worksheet.Cell("A1").Value = "Отчет по клиентам с наибольшими заказами";
                worksheet.Cell("A2").Value = $"Период: с {startDate.Value:dd.MM.yyyy} по {endDate.Value:dd.MM.yyyy}";
                worksheet.Range("A1:C1").Row(1).Merge().Style.Font.SetBold().Font.SetFontSize(14);
                worksheet.Range("A2:C2").Row(1).Merge().Style.Font.SetItalic().Font.SetFontSize(11);
                
                worksheet.Cell("A4").Value = "ФИО Клиента";
                worksheet.Cell("B4").Value = "Кол-во заказов";
                worksheet.Cell("C4").Value = "Общая сумма покупок (руб.)";

                var headerRange = worksheet.Range("A4:C4");
                headerRange.Style.Font.SetBold();
                headerRange.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                
                int currentRow = 5;
                foreach (var item in reportData)
                {
                    worksheet.Cell(currentRow, 1).Value = item.FullName;
                    worksheet.Cell(currentRow, 2).Value = item.OrdersCount;
                    worksheet.Cell(currentRow, 3).Value = item.TotalSpent;

                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0.00";
                    currentRow++;
                }
                
                worksheet.Range(4, 1, currentRow - 1, 3).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                worksheet.Range(4, 1, currentRow - 1, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                
                worksheet.Columns().AdjustToContents();
                
                workbook.SaveAs(filePath);
            });

            NotificationService.Notify("Успех!", $"Отчёт сохранен на Рабочий стол:\n{Path.GetFileName(filePath)}", NotificationType.Success);
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка отчёта", ex.Message, NotificationType.Error);
        }
    }
}

