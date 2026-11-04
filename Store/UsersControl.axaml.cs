using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material3.Avalonia.Controls;
using Microsoft.EntityFrameworkCore;
using Store.Context;
using Store.Entities;

namespace Store;

public partial class UsersControl : UserControl
{
    private ObservableCollection<CartItem> Cart { get; } = [];
    private Client _client;
    private LoginControl? _loginControl;
    public UsersControl(Client client)
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
    
    private async void LoadClientsData()
    {
        try
        {
            var currentClient = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                return await db.Clients.FirstOrDefaultAsync(c => c.Id == _client.Id);
            });
            
            AccountFullName.Text = currentClient?.FullName;
            AccountEmail.Text = currentClient?.Email;
            AccountPhone.Text = currentClient?.PhoneNumber;
        }
        catch (Exception ex)
        {
            NotificationService.Notify("Ошибка!",$"Извините, возникла следующая ошибка, расскажите о ней разработчику пожалуйста! {ex.Message}",  NotificationType.Error);
        }
    }
    
    private async void LoadOrdersData()
    {
        try
        {
            var ordersList = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                return await db.Orders
                    .Include(o => o.Client)
                    .Where(o => o.ClientId == _client.Id)
                    .ToListAsync();
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
            var productsNotNull = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                return await db.Products.Where(p => p.StockQuantity > 0).ToListAsync();

            });
            
            OrderProductComboBox.ItemsSource = productsNotNull;
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
    private void OnSearchDateChangedOrders(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        ApplyFilterAsyncOrders();
    }

    private void OnResetSearchClickOrders(object? sender, RoutedEventArgs e)
    {
        SearchDatePicker.SelectedDate = null;
        ApplyFilterAsyncOrders(); 
    }

    private async void ApplyFilterAsyncOrders()
    {
        try
        {
            DateTime? targetDate = SearchDatePicker.SelectedDate?.DateTime.Date;

            var filteredOrders = await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                var query = db.Orders.Include(o => o.Client).Where(o => o.ClientId == _client.Id).AsQueryable();

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
            if (Cart.Count == 0)
            {
                NotificationService.Notify("Внимание", "Корзина пуста. Нечего оформлять.", NotificationType.Warning);
                return;
            }
            
            decimal totalAmount = Cart.Sum(item => item.Total);

            await Task.Run(async () =>
            {
                await using var db = new StoreDB();
                
                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    var newOrder = new Order
                    {
                        ClientId = _client.Id,
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
    
}