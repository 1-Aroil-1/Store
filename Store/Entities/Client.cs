using System.Collections.Generic;

namespace Store.Entities;

public partial class Client
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int Role { get; set; }
    
    public string RoleName => Role == 1 ? "Администратор" : "Пользователь"; 

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
