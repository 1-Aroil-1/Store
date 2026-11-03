using System;
using System.Collections.Generic;

namespace Store.Entities;

public partial class Order
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
