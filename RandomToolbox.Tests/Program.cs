using RandomToolbox.App.Models;
using RandomToolbox.App.Services;

var random = new SecureRandomService();

Should("随机数字数量正确", () => random.GenerateNumbers(new NumberConfig { Min = 1, Max = 10, Count = 5 }).Values.Count == 5);
Should("不重复数量超限会报错", () => Throws(() => random.GenerateNumbers(new NumberConfig { Min = 1, Max = 3, Count = 5, AllowDuplicates = false })));
Should("骰子范围正确", () => random.RollDice(3, 6).Values.All(x => x is >= 1 and <= 6));
Should("表达式可计算", () => random.RollExpression("2D6+3", "正常").Result >= 5);
Should("抽签数量正确", () => random.Draw(["张三", "李四", "王五"], 2, false).Values.Count == 2);
Should("排序不丢项目", () => random.ShuffleItems(["a", "b", "c"]).Order().SequenceEqual(new[] { "a", "b", "c" }));
Should("分组数量正确", () => random.GroupItems(["a", "b", "c", "d"], "按组数分组", 2).Groups.Count == 2);

Console.WriteLine("全部核心逻辑测试通过。");

static void Should(string name, Func<bool> test)
{
    if (!test()) throw new Exception($"测试失败：{name}");
    Console.WriteLine($"通过：{name}");
}

static bool Throws(Action action)
{
    try
    {
        action();
        return false;
    }
    catch
    {
        return true;
    }
}
