using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using RandomToolbox.App.Models;

namespace RandomToolbox.App.Services;

public sealed class SecureRandomService
{
    private static readonly Regex DiceExpressionRegex = new(@"^\s*(\d*)[dD](\d+)\s*([+-]\s*\d+)?\s*$", RegexOptions.Compiled);

    public NumberResult GenerateNumbers(NumberConfig config)
    {
        if (config.Count <= 0) throw new ArgumentException("生成数量必须大于 0。");
        if (config.Min > config.Max) throw new ArgumentException("最小值不能大于最大值。");
        if (config.Count > 10000) throw new ArgumentException("一次最多生成 10000 个结果。");
        if (config.DecimalPlaces is < 0 or > 8) throw new ArgumentException("小数位数需要在 0 到 8 之间。");

        var values = new List<decimal>(config.Count);
        if (config.GenerateInteger)
        {
            var min = decimal.ToInt64(decimal.Ceiling(config.Min));
            var max = decimal.ToInt64(decimal.Floor(config.Max));
            if (min > max) throw new ArgumentException("当前范围内没有可用整数。");
            var available = max - min + 1;
            if (!config.AllowDuplicates && config.Count > available)
                throw new ArgumentException($"不允许重复时，当前范围最多只能生成 {available} 个整数。");

            if (config.AllowDuplicates)
            {
                for (var i = 0; i < config.Count; i++)
                    values.Add(RandomInt64(min, max));
            }
            else
            {
                if (available > 1000000) throw new ArgumentException("超大范围不重复生成请缩小范围或允许重复。");
                var pool = Enumerable.Range(0, checked((int)available)).Select(i => min + i).ToList();
                Shuffle(pool);
                values.AddRange(pool.Take(config.Count).Select(x => (decimal)x));
            }
        }
        else
        {
            var seen = new HashSet<decimal>();
            var attempts = 0;
            while (values.Count < config.Count)
            {
                attempts++;
                if (attempts > config.Count * 100)
                    throw new ArgumentException("当前小数精度下可用结果不足，请增加小数位数或允许重复。");

                var value = RandomDecimal(config.Min, config.Max, config.DecimalPlaces);
                if (config.AllowDuplicates || seen.Add(value))
                    values.Add(value);
            }
        }

        values = config.SortMode switch
        {
            "升序" => values.OrderBy(x => x).ToList(),
            "降序" => values.OrderByDescending(x => x).ToList(),
            _ => values
        };

        var text = values.Select(v => config.GenerateInteger
            ? decimal.ToInt64(v).ToString(CultureInfo.InvariantCulture)
            : v.ToString($"F{config.DecimalPlaces}", CultureInfo.InvariantCulture)).ToList();
        return new NumberResult(text);
    }

    public DiceRollResult RollDice(int count, int sides)
    {
        if (count <= 0) throw new ArgumentException("骰子数量必须大于 0。");
        if (count > 1000) throw new ArgumentException("一次最多投掷 1000 颗骰子。");
        if (sides < 2) throw new ArgumentException("骰子面数不能小于 2。");

        var values = Enumerable.Range(0, count).Select(_ => RandomInt32(1, sides)).ToList();
        var display = string.Join(Environment.NewLine, values.Select((v, i) => $"骰子{i + 1}：{v}"));
        return new DiceRollResult(sides, values, values.Sum(), values.Max(), values.Min(), display);
    }

    public DiceExpressionResult RollExpression(string expression, string keepMode)
    {
        if (string.IsNullOrWhiteSpace(expression)) throw new ArgumentException("请输入骰子表达式。");
        var match = DiceExpressionRegex.Match(expression);
        if (!match.Success) throw new ArgumentException("表达式格式示例：2D6+3、1D20+5、3D8-2。");

        var count = string.IsNullOrWhiteSpace(match.Groups[1].Value) ? 1 : int.Parse(match.Groups[1].Value);
        var sides = int.Parse(match.Groups[2].Value);
        var modifier = 0;
        if (match.Groups[3].Success)
            modifier = int.Parse(match.Groups[3].Value.Replace(" ", ""), CultureInfo.InvariantCulture);

        var roll = RollDice(count, sides);
        var result = keepMode switch
        {
            "取最高值" => roll.Values.Max() + modifier,
            "取最低值" => roll.Values.Min() + modifier,
            _ => roll.Values.Sum() + modifier
        };

        var parts = keepMode switch
        {
            "取最高值" => new List<string> { $"最高值 {roll.Values.Max()}" },
            "取最低值" => new List<string> { $"最低值 {roll.Values.Min()}" },
            _ => roll.Values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList()
        };
        if (modifier > 0) parts.Add($"+ {modifier}");
        if (modifier < 0) parts.Add($"- {Math.Abs(modifier)}");

        return new DiceExpressionResult(expression, roll.Values, modifier, result, $"{string.Join(" + ", parts).Replace("+ -", "-")} = {result}");
    }

    public DrawResult Draw(IReadOnlyList<string> items, int count, bool allowRepeat)
    {
        if (items.Count == 0) throw new ArgumentException("请先输入抽签项目。");
        if (count <= 0) throw new ArgumentException("抽取数量必须大于 0。");
        if (!allowRepeat && count > items.Count) throw new ArgumentException($"不重复抽取时最多只能抽取 {items.Count} 个。");

        var result = new List<string>();
        if (allowRepeat)
        {
            for (var i = 0; i < count; i++)
                result.Add(items[RandomInt32(0, items.Count - 1)]);
        }
        else
        {
            var pool = items.ToList();
            Shuffle(pool);
            result.AddRange(pool.Take(count));
        }

        return new DrawResult(result);
    }

    public IReadOnlyList<string> ShuffleItems(IReadOnlyList<string> items)
    {
        if (items.Count == 0) throw new ArgumentException("请先输入需要排序的内容。");
        var copy = items.ToList();
        Shuffle(copy);
        return copy;
    }

    public GroupResult GroupItems(IReadOnlyList<string> items, string mode, int value)
    {
        if (items.Count < 2) throw new ArgumentException("随机分组至少需要 2 个项目。");
        if (value <= 0) throw new ArgumentException("分组参数必须大于 0。");
        var shuffled = ShuffleItems(items).ToList();
        var groups = new List<List<string>>();

        if (mode == "按组数分组")
        {
            if (value > items.Count) throw new ArgumentException("组数不能超过项目数量。");
            for (var i = 0; i < value; i++) groups.Add([]);
            for (var i = 0; i < shuffled.Count; i++)
                groups[i % value].Add(shuffled[i]);
        }
        else
        {
            if (value > items.Count) throw new ArgumentException("每组人数不能超过项目数量。");
            for (var i = 0; i < shuffled.Count; i += value)
                groups.Add(shuffled.Skip(i).Take(value).ToList());
        }

        return new GroupResult(groups);
    }

    public string Coin() => RandomInt32(0, 1) == 0 ? "正面" : "反面";
    public string YesNo() => RandomInt32(0, 1) == 0 ? "Yes" : "No";

    private static int RandomInt32(int minInclusive, int maxInclusive) =>
        RandomNumberGenerator.GetInt32(minInclusive, checked(maxInclusive + 1));

    private static long RandomInt64(long minInclusive, long maxInclusive)
    {
        var range = (decimal)maxInclusive - minInclusive + 1;
        var sample = RandomNumberGenerator.GetInt32(0, int.MaxValue) / (decimal)int.MaxValue;
        return minInclusive + (long)Math.Floor(sample * range);
    }

    private static decimal RandomDecimal(decimal min, decimal max, int places)
    {
        var sample = RandomNumberGenerator.GetInt32(0, int.MaxValue) / (decimal)int.MaxValue;
        var value = min + (max - min) * sample;
        return Math.Round(value, places, MidpointRounding.AwayFromZero);
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var j = RandomInt32(0, i);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
