using System;
using System.Collections.Generic;

namespace Day_2.models;

public partial class Account
{
    public string AccountNumber { get; set; } = null!;

    public int CustomerId { get; set; }

    public decimal Balance { get; set; }

    public DateTime LastAccessed { get; set; }

    public string Status { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
