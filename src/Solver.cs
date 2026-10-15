using System.Diagnostics;

namespace VoltorbLens;

// Every solution is equally weighted; no level or game-generation prior is used.
public sealed class Solver
{
    // Hoeffding + union bound over 25 cells x 2 displayed events, fixed sample size.
    // P(any absolute error > .03) <= 100 exp(-2 N .03²) <= .05.
    public const int SampleTarget = 4223;
    readonly Board board;
    readonly int[][][] choices;
    readonly int[] order;
    public Solver(Board board)
    {
        board.Validate(); this.board = board;
        choices = Enumerable.Range(0,5).Select(r => Enumerable.Range(0,1024)
            .Select(code => Enumerable.Range(0,5).Select(c => (code >> (2*c)) & 3).ToArray())
            .Where(a => a.Sum()==board.Rows[r].Sum && a.Count(v=>v==0)==board.Rows[r].Bombs &&
                a.Where((v,c)=>board.Cells[r*5+c]>=0 && board.Cells[r*5+c]!=v).Count()==0).ToArray()).ToArray();
        order = Enumerable.Range(0,5).OrderBy(r=>choices[r].Length).ToArray();
    }
    public async Task<SolveResult> SolveAsync(CancellationToken cancellation, TimeSpan? exactBudget = null)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        var sw = Stopwatch.StartNew();
        var exact = Task.Run(()=>Enumerate(stop.Token), stop.Token);
        var delay = Task.Delay(exactBudget ?? TimeSpan.FromSeconds(3), stop.Token);
        Task<SolveResult>? sample = null;
        try
        {
            if (await Task.WhenAny(exact,delay) == exact) return (await exact) with { Seconds=sw.Elapsed.TotalSeconds };
            cancellation.ThrowIfCancellationRequested();
            // Keep exhaustive search alive to detect inconsistent boards / finish difficult cases.
            // Sampling is independent of enumeration order and never uses a truncated DFS prefix.
            sample = Task.Run(()=>Sample(stop.Token), stop.Token);
            var winner = await Task.WhenAny(exact,sample);
            return (await winner) with { Seconds=sw.Elapsed.TotalSeconds };
        }
        finally
        {
            stop.Cancel();
            try { await exact; } catch (OperationCanceledException) { }
            if (sample is not null) { try { await sample; } catch (OperationCanceledException) { } }
        }
    }
    public SolveResult Enumerate(CancellationToken ct)
    {
        long count=0, nodes=0; var bombs=new long[25]; var rewards=new long[25];
        var sum=new int[5]; var zero=new int[5]; var values=new int[25];
        void Visit(int depth)
        {
            if ((++nodes & 1023)==0) ct.ThrowIfCancellationRequested();
            if (depth==5) { Accumulate(values,bombs,rewards); count++; return; }
            int r=order[depth], remaining=4-depth;
            foreach(var row in choices[r])
            {
                bool valid=true;
                for(int c=0;c<5;c++)
                {
                    int s=board.Columns[c].Sum-sum[c]-row[c];
                    int z=board.Columns[c].Bombs-zero[c]-(row[c]==0?1:0);
                    if(z<0 || z>remaining || s<remaining-z || s>3*(remaining-z)) { valid=false; break; }
                }
                if(!valid) continue;
                for(int c=0;c<5;c++) { sum[c]+=row[c]; zero[c]+=row[c]==0?1:0; values[r*5+c]=row[c]; }
                Visit(depth+1);
                for(int c=0;c<5;c++) { sum[c]-=row[c]; zero[c]-=row[c]==0?1:0; }
            }
        }
        ct.ThrowIfCancellationRequested(); Visit(0);
        if(count==0) throw new InvalidOperationException("힌트와 열린 카드에 맞는 보드가 없습니다. 인식 영역을 확인하세요.");
        return Result(true,count,bombs,rewards);
    }
    public SolveResult Sample(CancellationToken ct, int seed = 0)
    {
        if(choices.Any(x=>x.Length==0)) throw new InvalidOperationException("열린 카드와 힌트가 일치하지 않습니다.");
        var rng=seed==0 ? new Random() : new Random(seed);
        var bombs=new long[25]; var rewards=new long[25]; var values=new int[25];
        int last=order[4];
        var lastCodes=choices[last].Select(row=>row.Select((v,c)=>v<<(2*c)).Sum()).ToHashSet();
        int accepted=0; long attempts=0;
        while(accepted<SampleTarget)
        {
            if((++attempts&1023)==0) ct.ThrowIfCancellationRequested();
            for(int k=0;k<4;k++) { int r=order[k]; var row=choices[r][rng.Next(choices[r].Length)]; Array.Copy(row,0,values,r*5,5); }
            int code=0; bool valid=true;
            for(int c=0;c<5;c++)
            {
                int s=0,z=0;
                for(int k=0;k<4;k++) { int v=values[order[k]*5+c]; s+=v; if(v==0) z++; }
                int vLast=board.Columns[c].Sum-s;
                if(vLast<0 || vLast>3 || z+(vLast==0?1:0)!=board.Columns[c].Bombs) { valid=false; break; }
                values[last*5+c]=vLast; code |= vLast<<(2*c);
            }
            if(!valid || !lastCodes.Contains(code)) continue;
            // Four independently uniform row choices uniquely determine the fifth.
            // All accepted boards therefore have identical proposal probability.
            Accumulate(values,bombs,rewards); accepted++;
        }
        ct.ThrowIfCancellationRequested();
        return Result(false,accepted,bombs,rewards);
    }
    static void Accumulate(int[] values,long[] b,long[] r) { for(int i=0;i<25;i++) { if(values[i]==0)b[i]++; if(values[i]>=2)r[i]++; } }
    static SolveResult Result(bool exact,long n,long[] b,long[] r) => new(exact,n,b.Select(v=>(double)v/n).ToArray(),r.Select(v=>(double)v/n).ToArray(),exact?0:Math.Sqrt(Math.Log(2000)/(2*n)),0);
}
