namespace VoltorbLens;

public sealed record Clue(int Sum, int Bombs);
public sealed record Board(Clue[] Rows, Clue[] Columns, int[] Cells)
{
    public string Key => HintKey + ":" + string.Join(',', Cells);
    public string HintKey => string.Join(';', Rows.Concat(Columns).Select(x => $"{x.Sum},{x.Bombs}"));
    public bool IsNewRoundFrom(Board old) => HintKey != old.HintKey || Cells.Where((v, i) => old.Cells[i] >= 0 && v != old.Cells[i]).Any();
    public void Validate()
    {
        if (Rows.Length != 5 || Columns.Length != 5 || Cells.Length != 25) throw new ArgumentException("보드 크기가 올바르지 않습니다.");
        if (Cells.Any(x => x < -1 || x > 3)) throw new ArgumentException("카드 값이 올바르지 않습니다.");
        foreach (var h in Rows.Concat(Columns))
            if (h.Bombs < 0 || h.Bombs > 5 || h.Sum < 5-h.Bombs || h.Sum > 3*(5-h.Bombs))
                throw new ArgumentException("행·열 힌트가 유효하지 않습니다.");
        if (Rows.Sum(x => x.Sum) != Columns.Sum(x => x.Sum) || Rows.Sum(x => x.Bombs) != Columns.Sum(x => x.Bombs))
            throw new ArgumentException("행·열 힌트 합계가 일치하지 않습니다.");
    }
    public bool Finished => Cells.Contains(0) || Cells.Where(x => x > 0).Sum(x=>x-1) == Rows.Sum(x=>x.Sum) - (25-Rows.Sum(x=>x.Bombs));
    public static Board FromValues(int[] values, int[]? visible = null) => new(
        Enumerable.Range(0,5).Select(r => new Clue(values.Skip(r*5).Take(5).Sum(), values.Skip(r*5).Take(5).Count(x=>x==0))).ToArray(),
        Enumerable.Range(0,5).Select(c => new Clue(Enumerable.Range(0,5).Sum(r=>values[r*5+c]), Enumerable.Range(0,5).Count(r=>values[r*5+c]==0))).ToArray(),
        visible ?? Enumerable.Repeat(-1,25).ToArray());
}

public sealed record SolveResult(bool Exact, long Count, double[] Bomb, double[] Reward, double Error, double Seconds);
public static class ProbabilityText
{
    public static string Format(double p, bool exact)
    {
        string prefix = exact ? "" : "≈";
        if (exact && p == 0) return "0%";
        if (exact && p == 1) return "100%";
        if (p < .001) return prefix + "<0.1%";
        if (p > .999) return prefix + ">99.9%";
        return prefix + (Math.Round(p*100,1) is var v ? Math.Clamp(v,.1,99.9).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : "") + "%";
    }
}
