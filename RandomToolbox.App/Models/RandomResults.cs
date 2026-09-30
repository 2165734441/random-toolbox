namespace RandomToolbox.App.Models;

public sealed record NumberResult(IReadOnlyList<string> Values);

public sealed record DiceRollResult(
    int Sides,
    IReadOnlyList<int> Values,
    int Total,
    int Max,
    int Min,
    string Display);

public sealed record DiceExpressionResult(
    string Expression,
    IReadOnlyList<int> Rolls,
    int Modifier,
    int Result,
    string Display);

public sealed record DrawResult(IReadOnlyList<string> Values);

public sealed record GroupResult(IReadOnlyList<IReadOnlyList<string>> Groups);
