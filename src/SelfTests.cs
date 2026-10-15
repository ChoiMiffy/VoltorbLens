using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace VoltorbLens;
public static class SelfTests
{
    public static Bitmap Fixture(Board board,string assets)
    {
        var image=new Bitmap(192,192);using var g=Graphics.FromImage(image);g.Clear(Color.FromArgb(41,165,107));
        void Blit(string name,int x,int y){using var src=new Bitmap(Path.Combine(assets,name+".png"));g.DrawImageUnscaled(src,x,y);}
        for(int i=0;i<25;i++){int v=board.Cells[i];Blit("tile_"+(v<0?"blank":v==0?"voltorb":v.ToString()),4+i%5*32,4+i/5*32);}
        void Hint(Clue h,int x,int y){using var bg=new SolidBrush(Color.FromArgb(255,190,100));g.FillRectangle(bg,x-2,y-1,24,27);Blit("number_bold_"+(h.Sum/10),x,y);Blit("number_bold_"+(h.Sum%10),x+8,y);Blit("number_bold_"+h.Bombs,x+8,y+13);}
        for(int i=0;i<5;i++){Hint(board.Rows[i],172,3+i*32);Hint(board.Columns[i],12+i*32,163);}
        return image;
    }
    public static int Run()
    {
        var log=new List<string>();int failures=0;
        void Check(string name,Action test){try{test();log.Add("PASS "+name);}catch(Exception e){failures++;log.Add("FAIL "+name+": "+e);}}
        void Assert(bool condition,string message="Assertion failed"){if(!condition)throw new Exception(message);}
        var values=Enumerable.Range(0,25).Select(i=>i/5==i%5?0:1).ToArray();var board=Board.FromValues(values);
        Check("Exact: 120 permutation boards, marginal bombs 20%",()=>{var s=new Solver(board).Enumerate(default);Assert(s.Count==120);Assert(s.Bomb.All(p=>Math.Abs(p-.2)<1e-12));Assert(s.Reward.All(p=>p==0));});
        Check("Opened card conditions reduce solution set to 96",()=>{var cells=(int[])board.Cells.Clone();cells[0]=1;var s=new Solver(board with{Cells=cells}).Enumerate(default);Assert(s.Count==96);Assert(s.Bomb[0]==0);Assert(Math.Abs(s.Bomb[1]-.25)<1e-12);});
        Check("Exact reward marginals",()=>{var b=Board.FromValues(values.Select(v=>v==0?3:1).ToArray());var s=new Solver(b).Enumerate(default);Assert(s.Bomb.All(p=>p==0));Assert(s.Reward.All(p=>p>0&&p<1));});
        Check("Independent rejection sample meets fixed Hoeffding budget",()=>{var s=new Solver(board).Sample(default,127);Assert(s.Count==4223&&!s.Exact);Assert(s.Error<=.03);Assert(s.Bomb.All(p=>Math.Abs(p-.2)<.03));});
        Check("Fully observed board has exactly one solution",()=>{var s=new Solver(Board.FromValues(values,values)).Enumerate(default);Assert(s.Count==1);Assert(s.Bomb[0]==1&&s.Bomb[1]==0);});
        Check("Inconsistent observation rejected",()=>{var cells=(int[])board.Cells.Clone();cells[0]=3;try{new Solver(board with{Cells=cells}).Enumerate(default);throw new Exception("not rejected");}catch(InvalidOperationException){}});
        Check("Cancellation is honored",()=>{using var cts=new CancellationTokenSource();cts.Cancel();try{new Solver(board).Enumerate(cts.Token);throw new Exception("not cancelled");}catch(OperationCanceledException){}});
        Check("Solve task terminates and returns exact when fast",()=>{var s=new Solver(board).SolveAsync(default).GetAwaiter().GetResult();Assert(s.Exact&&s.Count==120);});
        Check("Active solver cancellation leaves no worker running",()=>{var dense=new Board(Enumerable.Repeat(new Clue(7,1),5).ToArray(),Enumerable.Repeat(new Clue(7,1),5).ToArray(),Enumerable.Repeat(-1,25).ToArray());using var cts=new CancellationTokenSource();var task=new Solver(dense).SolveAsync(cts.Token,TimeSpan.FromMinutes(1));cts.CancelAfter(5);try{task.GetAwaiter().GetResult();throw new Exception("did not cancel");}catch(OperationCanceledException){}});
        Check("Timeout route returns a complete unbiased estimate or exact result",()=>{var dense=new Board(Enumerable.Repeat(new Clue(7,1),5).ToArray(),Enumerable.Repeat(new Clue(7,1),5).ToArray(),Enumerable.Repeat(-1,25).ToArray());using var cts=new CancellationTokenSource(TimeSpan.FromSeconds(8));var result=new Solver(dense).SolveAsync(cts.Token,TimeSpan.Zero).GetAwaiter().GetResult();Assert(result.Exact||result.Count==Solver.SampleTarget);Assert(result.Bomb.All(p=>Math.Abs(p-.2)<.03));log.Add("INFO forced timeout route: "+(result.Exact?"exact won race":"sample completed"));});
        Check("Round: initial midgame, restart, auto toggle",()=>{var fresh=Board.FromValues(values.Select(v=>v==0?3:1).ToArray());var mid=fresh with{Cells=Enumerable.Range(0,25).Select(i=>i==1?1:-1).ToArray()};var t=new RoundTracker();Assert(t.Accept(mid,false));Assert(!t.Accept(fresh,false));Assert(t.AwaitingStart);t.Reset();Assert(t.Accept(mid,true));Assert(t.Accept(fresh,true));});
        Check("Win detection requires all rewards, not all ones",()=>{var v=Enumerable.Repeat(1,25).ToArray();v[0]=2;v[1]=0;var b=Board.FromValues(v);Assert(!b.Finished);b.Cells[0]=2;Assert(b.Finished);});
        Check("Resize retains letterbox offsets and negative monitor coordinates",()=>{var s=new Settings{ClientWidth=800,ClientHeight=600,X=100,Y=100,Width=192,Height=192};Assert(s.Map(new Rectangle(-1600,0,1600,1200))==new Rectangle(-1400,200,384,384));Assert(s.Map(new Rectangle(0,0,1000,600))==new Rectangle(200,100,192,192));});
        Check("Approximate 0/1 never formatted as certainty",()=>{Assert(ProbabilityText.Format(0,false)=="≈<0.1%");Assert(ProbabilityText.Format(1,false)=="≈>99.9%");Assert(ProbabilityText.Format(.9999,true)==">99.9%");});
        string assets=Path.Combine(AppContext.BaseDirectory,"assets");
        var varied=Enumerable.Range(0,25).Select(i=>i%4).ToArray();var mixed=Board.FromValues(varied,Enumerable.Range(0,25).Select(i=>i%3==0?-1:varied[i]).ToArray());
        Check("Recognition: all card states and clue digits",()=>{using var reader=new Recognition(assets);using var b=Fixture(mixed,assets);Assert(reader.Read(b).Key==mixed.Key);});
        Check("Recognition: unused bottom padding may be absent",()=>{using var reader=new Recognition(assets);using var b=Fixture(mixed,assets);using var cropped=b.Clone(new Rectangle(0,0,192,185),System.Drawing.Imaging.PixelFormat.Format32bppArgb);Assert(reader.ReadRegion(cropped,new Rectangle(0,0,192,192)).Key==mixed.Key);});
        Check("Recognition: clipped clue pixels are rejected",()=>{using var reader=new Recognition(assets);using var b=Fixture(mixed,assets);using var cropped=b.Clone(new Rectangle(0,0,192,179),System.Drawing.Imaging.PixelFormat.Format32bppArgb);bool rejected=false;try{reader.ReadRegion(cropped,new Rectangle(0,0,192,192));}catch(InvalidOperationException){rejected=true;}Assert(rejected);});
        Check("Recognition: scaled 125%, 150%, 200%, 300%",()=>{using var reader=new Recognition(assets);using var b=Fixture(mixed,assets);foreach(double scale in new[]{1.25,1.5,2.0,3.0}){using var resized=new Bitmap((int)(192*scale),(int)(192*scale));using(var g=Graphics.FromImage(resized)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(b,new Rectangle(0,0,resized.Width,resized.Height));}Assert(reader.Read(resized).Key==mixed.Key,"scale "+scale);}});
        Check("Recognition refuses empty/transition screen",()=>{using var reader=new Recognition(assets);using var b=new Bitmap(192,192);try{reader.Read(b);throw new Exception("accepted black image");}catch(InvalidOperationException){}});
        Check("Recognition: bilinear 200% screen",()=>{using var reader=new Recognition(assets);using var b=Fixture(mixed,assets);using var resized=new Bitmap(384,384);using(var g=Graphics.FromImage(resized)){g.InterpolationMode=InterpolationMode.Bilinear;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(b,new Rectangle(0,0,384,384));}Assert(reader.Read(resized).Key==mixed.Key);});
        Check("Region registration corrects 4px drag offset",()=>{using var reader=new Recognition(assets);using var b=Fixture(mixed,assets);using var canvas=new Bitmap(224,224);using(var g=Graphics.FromImage(canvas)){g.Clear(Color.Black);g.DrawImageUnscaled(b,16,16);}var found=reader.Align(canvas,new Rectangle(20,12,192,192));Assert(found.Board.Key==mixed.Key);Assert(Math.Abs(found.Region.X-16)<=1&&Math.Abs(found.Region.Y-16)<=1);});
        Check("Independent brute-force oracle over partially open boards",()=>{var rng=new Random(731);for(int trial=0;trial<20;trial++){var v=Enumerable.Range(0,25).Select(_=>rng.Next(4)).ToArray();var cells=(int[])v.Clone();int[] holes={0,1,5,6};foreach(int i in holes)cells[i]=-1;var b=Board.FromValues(v,cells);long count=0;var bombs=new long[25];var reward=new long[25];for(int code=0;code<256;code++){var test=(int[])v.Clone();for(int k=0;k<4;k++)test[holes[k]]=(code>>(2*k))&3;var possible=Board.FromValues(test);if(possible.HintKey!=b.HintKey)continue;count++;for(int i=0;i<25;i++){if(test[i]==0)bombs[i]++;if(test[i]>=2)reward[i]++;}}var exact=new Solver(b).Enumerate(default);Assert(exact.Count==count);for(int i=0;i<25;i++){Assert(Math.Abs(exact.Bomb[i]-(double)bombs[i]/count)<1e-12);Assert(Math.Abs(exact.Reward[i]-(double)reward[i]/count)<1e-12);}}});
        Check("Bounded benchmark / cancellation on dense board",()=>{var dense=new Board(Enumerable.Repeat(new Clue(7,1),5).ToArray(),Enumerable.Repeat(new Clue(7,1),5).ToArray(),Enumerable.Repeat(-1,25).ToArray());using var cts=new CancellationTokenSource(TimeSpan.FromSeconds(8));var sw=Stopwatch.StartNew();try{var answer=new Solver(dense).SolveAsync(cts.Token).GetAwaiter().GetResult();log.Add($"BENCH dense: {(answer.Exact?"exact":"sample")} {answer.Count:N0}, {sw.Elapsed.TotalSeconds:0.000}s");Assert(answer.Exact||answer.Error<=.03);}catch(OperationCanceledException){Assert(sw.Elapsed.TotalSeconds<9);log.Add("BENCH dense: cancelled safely at 8 seconds");}});
        Check("Controller renders",()=>{using var form=new MainForm();form.Opacity=0;form.Show();Application.DoEvents();using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(AppContext.BaseDirectory,"ui-preview.png"));form.Hide();});
        Check("Window capture: live board updates under visible click-through overlay",()=>{
            using var fixture=Fixture(mixed,assets);
            using var window=new Form{StartPosition=FormStartPosition.Manual,Location=new Point(80,80),ClientSize=new Size(192,192),BackgroundImage=fixture,ShowInTaskbar=false,TopMost=true};
            window.Show();Application.DoEvents();Native.DwmFlush();
            var rect=Native.ClientBounds(window.Handle);
            using var reader=new Recognition(assets);
            using var overlay=new Overlay();
            overlay.Render(rect,mixed,new Solver(mixed).Enumerate(default),new Settings());
            overlay.SetCaptureExcluded(false);Application.DoEvents();Native.DwmFlush();
            int visibilityChanges=0;overlay.VisibleChanged+=(_,_)=>visibilityChanges++;
            Assert(!overlay.CaptureExcluded,"broadcast overlay excluded");
            Assert(Native.GetAncestor(Native.WindowFromPoint(new Point(rect.X+20,rect.Y+20)),2)==window.Handle,"overlay intercepted hit test");
            for(int i=0;i<8;i++){
                using var captured=Native.CaptureWindow(window.Handle,rect,rect);
                Assert(reader.Read(captured).Key==mixed.Key,"overlay polluted recognition");
                Assert(overlay.Visible,"overlay hidden during capture");
            }
            var changed=mixed with{Cells=(int[])mixed.Cells.Clone()};changed.Cells[0]=varied[0];
            using var next=Fixture(changed,assets);window.BackgroundImage=next;window.Refresh();Application.DoEvents();
            using(var captured=Native.CaptureWindow(window.Handle,rect,rect))Assert(reader.Read(captured).Key==changed.Key,"captured stale board");
            Assert(visibilityChanges==0,"overlay flickered during repeated captures");
            overlay.Hide();window.Hide();
        });
        log.Add($"{log.Count(x=>x.StartsWith("PASS "))} passed, {failures} failed");File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"test-results.txt"),log);return failures==0?0:1;
    }
}

