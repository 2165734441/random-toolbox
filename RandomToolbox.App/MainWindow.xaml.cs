using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RandomToolbox.App.Models;
using RandomToolbox.App.Services;

namespace RandomToolbox.App;

public partial class MainWindow : Window
{
    private readonly StorageService _storage = new();
    private readonly SecureRandomService _random = new();
    private AppData _data = new();
    private readonly List<string> _drawOriginalItems = [];
    private readonly List<string> _drawRemainingItems = [];
    private string _currentPage = "随机数字";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _data = _storage.Load();
        Width = Math.Max(_data.Window.Width, MinWidth);
        Height = Math.Max(_data.Window.Height, MinHeight);
        LoadUiFromData();
        RefreshHistoryGrid();
        RefreshPresetGrid();
        SelectPage(_data.Settings.OpenLastPageOnStart ? _data.Window.LastPage : "随机数字");
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveRecentConfig();
        _data.Window.Width = Width;
        _data.Window.Height = Height;
        _data.Window.LastPage = _currentPage;
        _storage.Save(_data);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded)
        {
            _data.Window.Width = Width;
            _data.Window.Height = Height;
        }
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is ListBoxItem item && item.Content is string page)
            SelectPage(page);
    }

    private void SelectPage(string page)
    {
        _currentPage = page;
        PageTitle.Text = page;
        foreach (var panel in new[] { PageNumber, PageDice, PageDraw, PageSort, PageHistory, PagePresets, PageSettings })
            panel.Visibility = Visibility.Collapsed;

        (page switch
        {
            "随机数字" => PageNumber,
            "骰子" => PageDice,
            "抽签" => PageDraw,
            "随机排序" => PageSort,
            "历史记录" => PageHistory,
            "预设管理" => PagePresets,
            "设置" => PageSettings,
            _ => PageNumber
        }).Visibility = Visibility.Visible;

        for (var i = 0; i < NavList.Items.Count; i++)
        {
            if ((NavList.Items[i] as ListBoxItem)?.Content as string == page)
            {
                NavList.SelectedIndex = i;
                break;
            }
        }

        _data.Window.LastPage = page;
        SaveQuietly();
    }

    private void LoadUiFromData()
    {
        var n = _data.Recent.Number;
        NumberMinBox.Text = n.Min.ToString();
        NumberMaxBox.Text = n.Max.ToString();
        NumberCountBox.Text = n.Count.ToString();
        NumberAllowRepeatCheck.IsChecked = n.AllowDuplicates;
        NumberIntegerCheck.IsChecked = n.GenerateInteger;
        SelectComboByText(NumberDecimalCombo, n.DecimalPlaces.ToString());
        SelectComboByText(NumberSortCombo, n.SortMode);

        var d = _data.Recent.Dice;
        DiceSidesCombo.ItemsSource = new[] { "4", "6", "8", "10", "12", "20", "100" };
        DiceSidesCombo.Text = d.Sides.ToString();
        DiceCountBox.Text = d.Count.ToString();
        DiceExpressionBox.Text = d.Expression;
        SelectComboByText(DiceKeepCombo, d.KeepMode);

        DrawItemsBox.Text = _data.Recent.Draw.ItemsText;
        DrawCountBox.Text = _data.Recent.Draw.Count.ToString();
        DrawAllowRepeatCheck.IsChecked = _data.Recent.Draw.AllowRepeat;
        ResetDrawPool();

        SortItemsBox.Text = _data.Recent.Sort.ItemsText;
        SelectComboByText(GroupModeCombo, _data.Recent.Sort.GroupMode);
        GroupValueBox.Text = _data.Recent.Sort.GroupMode == "按每组人数分组"
            ? _data.Recent.Sort.GroupSize.ToString()
            : _data.Recent.Sort.GroupCount.ToString();

        SettingOpenLastPageCheck.IsChecked = _data.Settings.OpenLastPageOnStart;
        SettingDiceAnimationCheck.IsChecked = _data.Settings.DiceAnimationEnabled;
        SettingSaveHistoryCheck.IsChecked = _data.Settings.SaveHistory;
        SelectComboByText(SettingHistoryLimitCombo, _data.Settings.HistoryLimit == 0 ? "无限" : _data.Settings.HistoryLimit.ToString());
        SelectComboByText(SettingThemeCombo, _data.Settings.Theme);
        PresetTypeCombo.SelectedIndex = 0;
        HistoryTypeCombo.SelectedIndex = 0;
        ApplyTheme();
        NumberIntegerCheck_Changed(this, new RoutedEventArgs());
    }

    private void SaveRecentConfig()
    {
        TryGetNumberConfig(out var number, false);
        if (number is not null) _data.Recent.Number = number;
        TryGetDiceConfig(out var dice, false);
        if (dice is not null) _data.Recent.Dice = dice;
        _data.Recent.Draw = new DrawConfig { ItemsText = DrawItemsBox.Text, Count = ParseIntOrDefault(DrawCountBox.Text, 1), AllowRepeat = DrawAllowRepeatCheck.IsChecked == true };
        var groupMode = ComboText(GroupModeCombo);
        _data.Recent.Sort = new SortConfig
        {
            ItemsText = SortItemsBox.Text,
            GroupMode = groupMode,
            GroupCount = groupMode == "按组数分组" ? ParseIntOrDefault(GroupValueBox.Text, 2) : 2,
            GroupSize = groupMode == "按每组人数分组" ? ParseIntOrDefault(GroupValueBox.Text, 3) : 3
        };
    }

    private async void GenerateNumbers_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!TryGetNumberConfig(out var config, true) || config is null) return;
            var result = _random.GenerateNumbers(config);
            NumberResultBox.Text = string.Join(Environment.NewLine, result.Values);
            _data.Recent.Number = config;
            AddHistory("随机数字", $"{config.Min}-{config.Max}，数量 {config.Count}，{config.SortMode}", string.Join("、", result.Values));
            ShowStatus("随机数字已生成");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        await Task.CompletedTask;
    }

    private async void RollDice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!TryGetDiceConfig(out var config, true) || config is null) return;
            if (_data.Settings.DiceAnimationEnabled)
                await AnimateDice(config.Sides);

            var result = _random.RollDice(config.Count, config.Sides);
            DiceResultBox.Text = $"骰子：{config.Count}D{config.Sides}{Environment.NewLine}{Environment.NewLine}{result.Display}{Environment.NewLine}{Environment.NewLine}总点数：{result.Total}{Environment.NewLine}最大值：{result.Max}{Environment.NewLine}最小值：{result.Min}";
            _data.Recent.Dice = config;
            AddHistory("骰子", $"{config.Count}D{config.Sides}", $"结果：{string.Join("、", result.Values)}；总计：{result.Total}");
            ShowStatus("投掷完成");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async void RollDiceExpression_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_data.Settings.DiceAnimationEnabled)
                await AnimateDice(20);
            var keepMode = ComboText(DiceKeepCombo);
            var result = _random.RollExpression(DiceExpressionBox.Text, keepMode);
            DiceResultBox.Text = $"表达式：{result.Expression}{Environment.NewLine}骰子结果：{string.Join("、", result.Rolls)}{Environment.NewLine}规则：{keepMode}{Environment.NewLine}最终：{result.Display}";
            _data.Recent.Dice.Expression = DiceExpressionBox.Text;
            _data.Recent.Dice.KeepMode = keepMode;
            AddHistory("骰子", $"{result.Expression}，{keepMode}", result.Display);
            ShowStatus("表达式已计算");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async Task AnimateDice(int sides)
    {
        for (var i = 0; i < 8; i++)
        {
            DiceResultBox.Text = $"滚动中... {_random.RollDice(1, Math.Max(2, sides)).Values[0]}";
            await Task.Delay(45);
        }
    }

    private async void Coin_Click(object sender, RoutedEventArgs e)
    {
        if (_data.Settings.DiceAnimationEnabled)
        {
            for (var i = 0; i < 8; i++)
            {
                DiceResultBox.Text = i % 2 == 0 ? "硬币翻转中... 正面" : "硬币翻转中... 反面";
                await Task.Delay(40);
            }
        }
        var result = _random.Coin();
        DiceResultBox.Text = $"抛硬币：{result}";
        AddHistory("骰子", "抛硬币", result);
        ShowStatus("硬币已抛出");
    }

    private void Draw_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var inputItems = GetLines(DrawItemsBox.Text);
            if (_drawOriginalItems.Count == 0 || !_drawOriginalItems.SequenceEqual(inputItems))
                ResetDrawPool();

            var count = ParsePositiveInt(DrawCountBox.Text, "抽取数量");
            var allowRepeat = DrawAllowRepeatCheck.IsChecked == true;
            var pool = allowRepeat ? _drawOriginalItems : _drawRemainingItems;
            var result = _random.Draw(pool, count, allowRepeat);
            if (!allowRepeat)
            {
                foreach (var item in result.Values)
                    _drawRemainingItems.Remove(item);
            }
            DrawResultBox.Text = string.Join(Environment.NewLine, result.Values.Select((v, i) => $"{i + 1}. {v}"));
            _data.Recent.Draw = new DrawConfig { ItemsText = DrawItemsBox.Text, Count = count, AllowRepeat = allowRepeat };
            AddHistory("抽签", $"数量 {count}，{(allowRepeat ? "允许重复" : "不重复")}", string.Join("、", result.Values));
            ShowStatus(!allowRepeat ? $"已抽取，剩余 {_drawRemainingItems.Count} 项" : "已抽取");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void DrawRestart_Click(object sender, RoutedEventArgs e)
    {
        ResetDrawPool();
        DrawResultBox.Clear();
        ShowStatus("抽签池已重新开始");
    }

    private void DrawRestore_Click(object sender, RoutedEventArgs e)
    {
        ResetDrawPool();
        ShowStatus("已恢复全部抽签项目");
    }

    private void Shuffle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var items = GetLines(SortItemsBox.Text);
            var result = _random.ShuffleItems(items);
            SortResultBox.Text = string.Join(Environment.NewLine, result.Select((v, i) => $"{i + 1}. {v}"));
            AddHistory("随机排序", $"{items.Count} 项", string.Join("、", result));
            ShowStatus("随机排序完成");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void Group_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var items = GetLines(SortItemsBox.Text);
            var mode = ComboText(GroupModeCombo);
            var value = ParsePositiveInt(GroupValueBox.Text, mode == "按组数分组" ? "组数" : "每组人数");
            var result = _random.GroupItems(items, mode, value);
            var sb = new StringBuilder();
            for (var i = 0; i < result.Groups.Count; i++)
            {
                sb.AppendLine($"第 {i + 1} 组");
                sb.AppendLine(string.Join("、", result.Groups[i]));
                sb.AppendLine();
            }
            SortResultBox.Text = sb.ToString().Trim();
            AddHistory("随机分组", $"{mode}：{value}", SortResultBox.Text);
            ShowStatus("随机分组完成");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void Quick_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag }) return;
        try
        {
            string result = tag switch
            {
                "1-10" => _random.GenerateNumbers(new NumberConfig { Min = 1, Max = 10, Count = 1 }).Values[0],
                "1-100" => _random.GenerateNumbers(new NumberConfig { Min = 1, Max = 100, Count = 1 }).Values[0],
                "1-1000" => _random.GenerateNumbers(new NumberConfig { Min = 1, Max = 1000, Count = 1 }).Values[0],
                "coin" => _random.Coin(),
                "yesno" => _random.YesNo(),
                "d6" => _random.RollDice(1, 6).Values[0].ToString(),
                "d20" => _random.RollDice(1, 20).Values[0].ToString(),
                _ => ""
            };
            QuickResultText.Text = result;
            AddHistory(tag.StartsWith('d') || tag == "coin" ? "骰子" : "随机数字", $"快捷 {tag}", result);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void NumberRange_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            var parts = tag.Split(',');
            NumberMinBox.Text = parts[0];
            NumberMaxBox.Text = parts[1];
        }
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name }) return;
        if (FindName(name) is TextBox box && !string.IsNullOrWhiteSpace(box.Text))
        {
            Clipboard.SetText(box.Text);
            ShowStatus("已复制到剪贴板");
        }
    }

    private void ClearResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name } && FindName(name) is TextBox box)
        {
            box.Clear();
            ShowStatus("已清空");
        }
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string type })
        {
            PresetNameBox.Text = $"{type}预设 {DateTime.Now:HHmmss}";
            AddPreset(type, PresetNameBox.Text);
        }
    }

    private void AddPresetFromManager_Click(object sender, RoutedEventArgs e)
    {
        var type = ComboText(PresetTypeCombo);
        var name = string.IsNullOrWhiteSpace(PresetNameBox.Text) ? $"{type}预设" : PresetNameBox.Text.Trim();
        AddPreset(type, name);
    }

    private void AddPreset(string type, string name)
    {
        try
        {
            var preset = BuildPreset(type, name);
            preset.Order = _data.Presets.Count == 0 ? 1 : _data.Presets.Max(p => p.Order) + 1;
            _data.Presets.Add(preset);
            RefreshPresetGrid();
            SaveQuietly();
            ShowStatus("预设已保存");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        if (PresetGrid.SelectedItem is not PresetItem preset) return;
        ApplyPreset(preset);
        ShowStatus("预设已应用");
    }

    private void UpdatePreset_Click(object sender, RoutedEventArgs e)
    {
        if (PresetGrid.SelectedItem is not PresetItem preset) return;
        try
        {
            var updated = BuildPreset(preset.Type, preset.Name);
            preset.Number = updated.Number;
            preset.Dice = updated.Dice;
            preset.Draw = updated.Draw;
            preset.Sort = updated.Sort;
            RefreshPresetGrid();
            SaveQuietly();
            ShowStatus("预设已修改");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RenamePreset_Click(object sender, RoutedEventArgs e)
    {
        if (PresetGrid.SelectedItem is not PresetItem preset) return;
        if (string.IsNullOrWhiteSpace(PresetNameBox.Text))
        {
            ShowError("请输入新的预设名称。");
            return;
        }
        preset.Name = PresetNameBox.Text.Trim();
        RefreshPresetGrid();
        SaveQuietly();
        ShowStatus("预设已重命名");
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (PresetGrid.SelectedItem is PresetItem preset)
        {
            _data.Presets.Remove(preset);
            NormalizePresetOrder();
            RefreshPresetGrid();
            SaveQuietly();
            ShowStatus("预设已删除");
        }
    }

    private void MovePreset_Click(object sender, RoutedEventArgs e)
    {
        if (PresetGrid.SelectedItem is not PresetItem preset || sender is not Button { Tag: string tag }) return;
        var direction = int.Parse(tag);
        var ordered = _data.Presets.OrderBy(p => p.Order).ToList();
        var index = ordered.IndexOf(preset);
        var target = index + direction;
        if (target < 0 || target >= ordered.Count) return;
        (ordered[index].Order, ordered[target].Order) = (ordered[target].Order, ordered[index].Order);
        RefreshPresetGrid();
        PresetGrid.SelectedItem = preset;
        SaveQuietly();
    }

    private PresetItem BuildPreset(string type, string name)
    {
        var preset = new PresetItem { Name = name, Type = type };
        switch (type)
        {
            case "随机数字":
                if (!TryGetNumberConfig(out var number, true) || number is null) throw new ArgumentException("随机数字设置无效。");
                preset.Number = number;
                break;
            case "骰子":
                if (!TryGetDiceConfig(out var dice, true) || dice is null) throw new ArgumentException("骰子设置无效。");
                dice.Expression = DiceExpressionBox.Text;
                dice.KeepMode = ComboText(DiceKeepCombo);
                preset.Dice = dice;
                break;
            case "抽签":
                preset.Draw = new DrawConfig { ItemsText = DrawItemsBox.Text, Count = ParsePositiveInt(DrawCountBox.Text, "抽取数量"), AllowRepeat = DrawAllowRepeatCheck.IsChecked == true };
                break;
            case "随机排序":
                preset.Sort = new SortConfig { ItemsText = SortItemsBox.Text, GroupMode = ComboText(GroupModeCombo), GroupCount = ParseIntOrDefault(GroupValueBox.Text, 2), GroupSize = ParseIntOrDefault(GroupValueBox.Text, 3) };
                break;
        }
        return preset;
    }

    private void ApplyPreset(PresetItem preset)
    {
        switch (preset.Type)
        {
            case "随机数字" when preset.Number is not null:
                _data.Recent.Number = preset.Number;
                LoadUiFromData();
                SelectPage("随机数字");
                break;
            case "骰子" when preset.Dice is not null:
                _data.Recent.Dice = preset.Dice;
                LoadUiFromData();
                SelectPage("骰子");
                break;
            case "抽签" when preset.Draw is not null:
                _data.Recent.Draw = preset.Draw;
                LoadUiFromData();
                SelectPage("抽签");
                break;
            case "随机排序" when preset.Sort is not null:
                _data.Recent.Sort = preset.Sort;
                LoadUiFromData();
                SelectPage("随机排序");
                break;
        }
    }

    private void HistoryFilter_Changed(object sender, EventArgs e) => RefreshHistoryGrid();

    private void CopyHistory_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is HistoryItem item)
        {
            Clipboard.SetText($"{item.Time:yyyy-MM-dd HH:mm:ss}\r\n{item.Type}\r\n{item.Settings}\r\n{item.Result}");
            ShowStatus("已复制到剪贴板");
        }
    }

    private void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is HistoryItem item)
        {
            _data.History.RemoveAll(h => h.Id == item.Id);
            RefreshHistoryGrid();
            SaveQuietly();
            ShowStatus("历史记录已删除");
        }
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _data.History.Clear();
        RefreshHistoryGrid();
        SaveQuietly();
        ShowStatus("历史记录已清空");
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _data.Settings.OpenLastPageOnStart = SettingOpenLastPageCheck.IsChecked == true;
        _data.Settings.DiceAnimationEnabled = SettingDiceAnimationCheck.IsChecked == true;
        _data.Settings.SaveHistory = SettingSaveHistoryCheck.IsChecked == true;
        _data.Settings.HistoryLimit = ComboText(SettingHistoryLimitCombo) == "无限" ? 0 : ParsePositiveInt(ComboText(SettingHistoryLimitCombo), "历史记录最大数量");
        _data.Settings.Theme = ComboText(SettingThemeCombo);
        ApplyTheme();
        SaveQuietly();
        ShowStatus("设置已保存");
    }

    private void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        _data.Settings = new AppSettings();
        LoadUiFromData();
        SaveQuietly();
        ShowStatus("已恢复默认设置");
    }

    private void NumberIntegerCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (NumberDecimalCombo is not null)
            NumberDecimalCombo.IsEnabled = NumberIntegerCheck.IsChecked != true;
    }

    private bool TryGetNumberConfig(out NumberConfig? config, bool showErrors)
    {
        config = null;
        try
        {
            if (!decimal.TryParse(NumberMinBox.Text, out var min)) throw new ArgumentException("最小值必须是数字。");
            if (!decimal.TryParse(NumberMaxBox.Text, out var max)) throw new ArgumentException("最大值必须是数字。");
            var count = ParsePositiveInt(NumberCountBox.Text, "生成数量");
            config = new NumberConfig
            {
                Min = min,
                Max = max,
                Count = count,
                AllowDuplicates = NumberAllowRepeatCheck.IsChecked == true,
                GenerateInteger = NumberIntegerCheck.IsChecked == true,
                DecimalPlaces = ParseIntOrDefault(ComboText(NumberDecimalCombo), 2),
                SortMode = ComboText(NumberSortCombo)
            };
            return true;
        }
        catch (Exception ex)
        {
            if (showErrors) ShowError(ex.Message);
            return false;
        }
    }

    private bool TryGetDiceConfig(out DiceConfig? config, bool showErrors)
    {
        config = null;
        try
        {
            var sides = ParsePositiveInt(DiceSidesCombo.Text, "骰子面数");
            var count = ParsePositiveInt(DiceCountBox.Text, "骰子数量");
            config = new DiceConfig
            {
                Sides = sides,
                Count = count,
                Expression = DiceExpressionBox.Text,
                KeepMode = ComboText(DiceKeepCombo)
            };
            return true;
        }
        catch (Exception ex)
        {
            if (showErrors) ShowError(ex.Message);
            return false;
        }
    }

    private void ResetDrawPool()
    {
        _drawOriginalItems.Clear();
        _drawOriginalItems.AddRange(GetLines(DrawItemsBox.Text));
        _drawRemainingItems.Clear();
        _drawRemainingItems.AddRange(_drawOriginalItems);
    }

    private void AddHistory(string type, string settings, string result)
    {
        HistoryService.Add(_data, type, settings, result);
        RefreshHistoryGrid();
        SaveQuietly();
    }

    private void RefreshHistoryGrid()
    {
        if (HistoryGrid is null) return;
        var query = _data.History.AsEnumerable();
        var type = HistoryTypeCombo is null ? "全部" : ComboText(HistoryTypeCombo);
        var search = HistorySearchBox?.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(type) && type != "全部")
            query = query.Where(h => h.Type == type);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(h => h.Type.Contains(search) || h.Settings.Contains(search) || h.Result.Contains(search));
        HistoryGrid.ItemsSource = query.ToList();
    }

    private void RefreshPresetGrid()
    {
        NormalizePresetOrder();
        PresetGrid.ItemsSource = _data.Presets.OrderBy(p => p.Order).ToList();
    }

    private void NormalizePresetOrder()
    {
        var ordered = _data.Presets.OrderBy(p => p.Order).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Order = i + 1;
    }

    private void ApplyTheme()
    {
        var dark = _data.Settings.Theme == "深色";
        Background = dark ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
    }

    private void SaveQuietly()
    {
        try
        {
            SaveRecentConfig();
            _storage.Save(_data);
        }
        catch
        {
            // 自动保存失败时不打断用户操作。
        }
    }

    private void ShowStatus(string message)
    {
        StatusText.Foreground = System.Windows.Media.Brushes.ForestGreen;
        StatusText.Text = message;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            StatusText.Text = "";
            timer.Stop();
        };
        timer.Start();
    }

    private void ShowError(string message)
    {
        StatusText.Foreground = System.Windows.Media.Brushes.Firebrick;
        StatusText.Text = message;
    }

    private static List<string> GetLines(string text) =>
        text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

    private static int ParsePositiveInt(string text, string name)
    {
        if (!int.TryParse(text, out var value)) throw new ArgumentException($"{name}必须是整数。");
        if (value <= 0) throw new ArgumentException($"{name}必须大于 0。");
        return value;
    }

    private static int ParseIntOrDefault(string text, int fallback) =>
        int.TryParse(text, out var value) ? value : fallback;

    private static string ComboText(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item) return item.Content?.ToString() ?? "";
        return combo.Text;
    }

    private static void SelectComboByText(ComboBox combo, string text)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem comboItem && comboItem.Content?.ToString() == text)
            {
                combo.SelectedItem = comboItem;
                return;
            }
        }
        combo.Text = text;
    }
}
