namespace RandomToolbox.App.Models;

public sealed class AppData
{
    public AppSettings Settings { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public RecentConfig Recent { get; set; } = new();
    public List<HistoryItem> History { get; set; } = [];
    public List<PresetItem> Presets { get; set; } = [];
}

public sealed class AppSettings
{
    public bool OpenLastPageOnStart { get; set; } = true;
    public bool DiceAnimationEnabled { get; set; } = true;
    public bool SaveHistory { get; set; } = true;
    public int HistoryLimit { get; set; } = 500;
    public string Theme { get; set; } = "跟随系统";
}

public sealed class WindowSettings
{
    public double Width { get; set; } = 1120;
    public double Height { get; set; } = 760;
    public string LastPage { get; set; } = "随机数字";
}

public sealed class RecentConfig
{
    public NumberConfig Number { get; set; } = new();
    public DiceConfig Dice { get; set; } = new();
    public DrawConfig Draw { get; set; } = new();
    public SortConfig Sort { get; set; } = new();
}

public sealed class NumberConfig
{
    public decimal Min { get; set; } = 1;
    public decimal Max { get; set; } = 100;
    public int Count { get; set; } = 5;
    public bool AllowDuplicates { get; set; } = true;
    public bool GenerateInteger { get; set; } = true;
    public int DecimalPlaces { get; set; } = 2;
    public string SortMode { get; set; } = "保持随机顺序";
}

public sealed class DiceConfig
{
    public int Sides { get; set; } = 6;
    public int Count { get; set; } = 1;
    public string Expression { get; set; } = "2D6+3";
    public string KeepMode { get; set; } = "正常";
}

public sealed class DrawConfig
{
    public string ItemsText { get; set; } = "";
    public int Count { get; set; } = 1;
    public bool AllowRepeat { get; set; }
}

public sealed class SortConfig
{
    public string ItemsText { get; set; } = "";
    public int GroupCount { get; set; } = 2;
    public int GroupSize { get; set; } = 3;
    public string GroupMode { get; set; } = "按组数分组";
}

public sealed class HistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Time { get; set; } = DateTime.Now;
    public string Type { get; set; } = "";
    public string Settings { get; set; } = "";
    public string Result { get; set; } = "";
}

public sealed class PresetItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Order { get; set; }
    public NumberConfig? Number { get; set; }
    public DiceConfig? Dice { get; set; }
    public DrawConfig? Draw { get; set; }
    public SortConfig? Sort { get; set; }
}
