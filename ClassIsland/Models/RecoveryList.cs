using System.Collections.Generic;

namespace ClassIsland.Models;

public class RecoveryWindowInfo
{
    public string WindowType { get; set; } = "";
    
    public string? Uri { get; set; }
}

public class RecoveryList
{
    public List<RecoveryWindowInfo> Windows { get; set; } = new();
}