using Avalonia.Controls;
using Store.Entities;

namespace Store;

public partial class UsersControl : UserControl
{
    private Client _client;
    public UsersControl(Client client)
    {
        InitializeComponent();
        _client = client;
    }
}