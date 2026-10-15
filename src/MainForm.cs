using System.Diagnostics;

namespace VoltorbLens;
public sealed class MainForm : Form
{
    readonly Settings settings=Settings.Load();
    readonly ComboBox windows=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=420};
    readonly Label status=new(){AutoSize=false,Width=480,Height=64,ForeColor=Color.FromArgb(153,220,255)};
    readonly Label statistics=new(){AutoSize=false,Width=480,Height=58};
    readonly TextBox preview=new(){Multiline=true,ReadOnly=true,Width=480,Height=180,Font=new Font("Consolas",11),ScrollBars=ScrollBars.Vertical};
    readonly Button start=new(){Text="계산 시작",AutoSize=true};
    readonly Overlay overlay=new();
    readonly ObsCaptureWindow obsCapture=new();
    readonly Recognition recognizer=new(Path.Combine(AppContext.BaseDirectory,"assets"));
    readonly System.Windows.Forms.Timer timer=new(){Interval=200};
    readonly RoundTracker rounds=new();
    CancellationTokenSource? solveCancellation;
    string? candidateKey, solvingKey; int stableFrames, generation;
    Board? current; SolveResult? result;
    bool running,busy,selecting; nint target; int recognitionFailures;
    Rectangle lastArea;
    bool manualRegion;
    DateTime nextSearch;

    public MainForm()
    {
        var embeddedIcon=Icon.ExtractAssociatedIcon(Environment.ProcessPath??Application.ExecutablePath);
        if(embeddedIcon is not null)Icon=embeddedIcon;
        Text="찌리리공 렌즈 · Voltorb Lens";ClientSize=new Size(525,690);MinimumSize=new Size(530,720);
        Font=new Font("Malgun Gothic",10);BackColor=Color.FromArgb(22,28,39);ForeColor=Color.White;AutoScaleMode=AutoScaleMode.Dpi;
        var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(18)};Controls.Add(flow);
        flow.Controls.Add(new Label{Text="찌리리공 렌즈",Font=new Font(Font.FontFamily,20,FontStyle.Bold),AutoSize=true});
        flow.Controls.Add(new Label{Text="화면에서 읽고, 확률로 확인하세요.",AutoSize=true,Margin=new Padding(3,3,3,14)});
        flow.Controls.Add(windows);
        var buttons=new FlowLayoutPanel{Width=480,Height=42};flow.Controls.Add(buttons);
        Button Add(string text,Action action){var b=new Button{Text=text,AutoSize=true,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(47,62,82)};b.Click+=(_,_)=>action();buttons.Controls.Add(b);return b;}
        Add("창 새로고침",RefreshWindows);Add("수동 영역",PickRegion);start.FlatStyle=FlatStyle.Flat;start.BackColor=Color.FromArgb(25,100,128);buttons.Controls.Add(start);start.Click+=(_,_)=>Start();Add("중지",()=>Stop("중지됨"));
        void Toggle(string text,bool initial,Action<bool> action){var c=new CheckBox{Text=text,Checked=initial,AutoSize=true};c.CheckedChanged+=(_,_)=>{action(c.Checked);Save();};flow.Controls.Add(c);}
        Toggle("새 라운드 자동 인식",settings.AutoRound,v=>settings.AutoRound=v);
        Toggle("OBS 캡처에 확률 표시",settings.ShowOverlayInCapture,v=>{settings.ShowOverlayInCapture=v;overlay.SetCaptureExcluded(!v);if(!v)obsCapture.Clear();Save();Render();});
        Toggle("제어창 항상 위에",settings.AlwaysTop,v=>{settings.AlwaysTop=v;TopMost=v;});TopMost=settings.AlwaysTop;
        Toggle("인식 결과 보기",settings.ShowRecognition,v=>{settings.ShowRecognition=v;preview.Visible=v;});
        void Slider(string text,int initial,Action<int> action){var label=new Label{Text=$"{text}: {100-initial}%",AutoSize=true};flow.Controls.Add(label);var s=new TrackBar{Minimum=0,Maximum=100,Value=100-initial,TickFrequency=25,Width=470,Height=40};s.ValueChanged+=(_,_)=>{label.Text=$"{text}: {s.Value}%";action(100-s.Value);Save();Render();};flow.Controls.Add(s);}
        Slider("숫자 투명도",settings.TextAlpha,v=>settings.TextAlpha=v);Slider("배경 투명도",settings.BackgroundAlpha,v=>settings.BackgroundAlpha=v);
        flow.Controls.Add(new Label{Text="계산 중에는 게임의 메모 기능을 사용하지 마세요.\n화면 비율 유지 · 창 모드 · 보드가 가려지지 않은 상태에서 사용",AutoSize=true,ForeColor=Color.FromArgb(255,207,128),Margin=new Padding(3,8,3,12)});
        flow.Controls.Add(status);flow.Controls.Add(statistics);flow.Controls.Add(preview);preview.Visible=settings.ShowRecognition;
        windows.SelectedIndexChanged+=(_,_)=>{Stop("창 선택됨 · 계산 시작을 누르면 보드를 자동으로 찾습니다.");manualRegion=false;settings.ClientWidth=0;if(windows.SelectedItem is WindowItem item){target=item.Handle;settings.ProcessName=item.Process;}};
        timer.Tick+=async(_,_)=>await Tick();timer.Start();RefreshWindows();
        status.Text="에뮬 창 선택 → 계산 시작 · 보드 영역은 자동으로 찾습니다.";
        FormClosing+=(_,_)=>{timer.Stop();Stop("종료");Save();};
    }
    void Save(){try{settings.Save();}catch(Exception ex){status.Text="설정 저장 실패: "+ex.Message;}}
    void RefreshWindows()
    {
        string wanted=settings.ProcessName;var items=Native.Windows();windows.Items.Clear();windows.Items.AddRange(items.Cast<object>().ToArray());
        var match=items.FindIndex(w=>w.Process==wanted);if(items.Count>0)windows.SelectedIndex=match<0?0:match;
    }
    void PickRegion()
    {
        if(target==0||!Native.IsWindow(target)){status.Text="먼저 에뮬 창을 선택하세요.";return;}
        Stop("영역 지정 중");Native.SetForegroundWindow(target);selecting=true;
        try
        {
            var client=Native.ClientBounds(target);if(client.Width<96||client.Height<96)return;
            using var picker=new RegionPicker(client);
            if(picker.ShowDialog()==DialogResult.OK){var r=picker.Selected;
                if(Math.Abs((double)r.Width/r.Height-1)>.08){status.Text="힌트를 포함한 6×6 정사각형 영역을 지정하세요.";return;}
                settings.ClientWidth=client.Width;settings.ClientHeight=client.Height;settings.X=r.X;settings.Y=r.Y;settings.Width=r.Width;settings.Height=r.Height;manualRegion=true;Save();status.Text="영역 저장 완료 · 계산 시작을 누르세요.";}
        }
        finally{selecting=false;}
    }
    void Start()
    {
        if(target==0||!Native.IsWindow(target)){status.Text="에뮬 창을 선택하세요.";return;}
        if(!manualRegion)settings.ClientWidth=0;
        recognitionFailures=0;nextSearch=DateTime.MinValue;
        Stop("");rounds.Reset();running=true;status.Text="인식 대기 · 에뮬 창을 활성화하세요.";Native.SetForegroundWindow(target);
    }
    void HideOverlays(){overlay.Hide();obsCapture.Clear();}
    void CancelCalculation(){generation++;solveCancellation?.Cancel();solveCancellation=null;solvingKey=null;result=null;HideOverlays();statistics.Text="";}
    void Stop(string message){running=false;CancelCalculation();candidateKey=null;stableFrames=0;current=null;status.Text=message;}
    async Task Tick()
    {
        if(!running||busy||selecting||IsDisposed)return;
        if(!Native.IsWindow(target)){Stop("에뮬 창이 종료되었습니다. 창을 다시 선택하세요.");return;}
        nint foreground=Native.GetForegroundWindow();
        if(Native.IsIconic(target)||!settings.ShowOverlayInCapture && foreground!=target && foreground!=Handle){HideOverlays();status.Text="대기 · 에뮬 창으로 돌아오면 다시 확인합니다.";candidateKey=null;stableFrames=0;return;}
        var capturedClient=Native.ClientBounds(target);
        bool searching=!settings.HasRegion||(!manualRegion&&recognitionFailures>=5);
        var area=searching?capturedClient:settings.Map(capturedClient);
        if(!searching&&!manualRegion&&!capturedClient.Contains(Recognition.RequiredPixels(area))){searching=true;area=capturedClient;}
        if(searching&&DateTime.UtcNow<nextSearch)return;
        if(area.Width<96||area.Height<96||!SystemInformation.VirtualScreen.Contains(searching?area:Recognition.RequiredPixels(area))){HideOverlays();status.Text="대기 · 카드와 힌트 숫자가 화면 안에 있어야 합니다.";return;}
        // A control panel covering the board must never be interpreted as game pixels.
        if(Visible && Bounds.IntersectsWith(area) && (foreground==Handle||TopMost)){HideOverlays();status.Text="제어창을 보드 밖으로 옮겨 주세요.";return;}
        busy=true;
        try
        {
            var expanded=searching?capturedClient:Rectangle.Inflate(area,Math.Max(12,area.Width/10),Math.Max(12,area.Height/10));expanded.Intersect(capturedClient);
            if(searching){nextSearch=DateTime.UtcNow.AddSeconds(1);status.Text="보드 위치 자동 탐색 중 · 게임 화면을 열어 주세요.";HideOverlays();}
            int capturedGeneration=generation;
            var capturedTarget=target;
            // Capture on a worker: PrintWindow can wait for the emulator to paint.
            // Never hide a broadcast-visible overlay just to read the game.
            bool directCapture=settings.ShowOverlayInCapture || (overlay.Visible && !overlay.CaptureExcluded);
            var expected=new Rectangle(area.X-expanded.X,area.Y-expanded.Y,area.Width,area.Height);
            var recognized=await Task.Run(()=>{
                using var capture=directCapture?Native.CaptureWindow(capturedTarget,capturedClient,expanded):Native.Capture(expanded);
                if(searching)return recognizer.FindBoard(capture);
                try{return(Board:recognizer.ReadRegion(capture,expected),Region:expected);}
                catch(Exception ex) when(ex is InvalidOperationException or ArgumentException){if(recognitionFailures%5!=0)throw;return recognizer.Align(capture,expected);}
            });
            if(!running||IsDisposed||capturedGeneration!=generation)return;
            if(Native.ClientBounds(target)!=capturedClient){CancelCalculation();candidateKey=null;stableFrames=0;return;}
            Board board=recognized.Board;recognitionFailures=0;
            if(searching||recognized.Region!=expected)
            {
                area=new Rectangle(expanded.X+recognized.Region.X,expanded.Y+recognized.Region.Y,recognized.Region.Width,recognized.Region.Height);
                var client=Native.ClientBounds(target);settings.ClientWidth=client.Width;settings.ClientHeight=client.Height;settings.X=area.X-client.X;settings.Y=area.Y-client.Y;settings.Width=area.Width;settings.Height=area.Height;Save();
            }
            if(area!=lastArea){HideOverlays();lastArea=area;}
            if(candidateKey!=board.Key){candidateKey=board.Key;stableFrames=1;CancelCalculation();status.Text="인식 중 · 화면 안정화 확인";return;}
            if(++stableFrames<3)return;
            current=board;UpdatePreview(board);
            if(!rounds.Accept(board,settings.AutoRound)){CancelCalculation();status.Text=rounds.AwaitingStart?"판 종료 / 새 판 대기 · 계산 시작을 눌러 주세요.":"판 종료 · 새 라운드 인식 대기";return;}
            if(result is not null)return;
            if(solvingKey==board.Key)return;
            solvingKey=board.Key;var cts=new CancellationTokenSource();solveCancellation=cts;int version=generation;
            status.Text="계산 중 · 3초 후에도 표본이 부족하면 계속 계산합니다.";
            _=Calculate(board,cts,version);
        }
        catch(Exception ex){if(running){recognitionFailures++;CancelCalculation();candidateKey=null;stableFrames=0;status.Text="인식 대기 · "+ex.Message;}}
        finally{busy=false;}
    }
    async Task Calculate(Board board,CancellationTokenSource cts,int version)
    {
        try
        {
            var answer=await new Solver(board).SolveAsync(cts.Token);
            if(cts.IsCancellationRequested||version!=generation||!running||IsDisposed)return;
            result=answer;status.Text=answer.Exact?"정확 계산 완료":"근삿값 표시 · 계산 종료";
            statistics.Text=answer.Exact?$"가능한 보드 {answer.Count:N0}개 · {answer.Seconds:0.00}초":$"독립 표본 {answer.Count:N0}개 · {answer.Seconds:0.00}초\n95% 동시 오차 한계 ±{answer.Error*100:0.00}%p (인식 오류 제외)";
            Render();
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(version==generation&&!IsDisposed){status.Text=ex.Message;HideOverlays();}}
        finally{if(ReferenceEquals(solveCancellation,cts))solveCancellation=null;cts.Dispose();}
    }
    void UpdatePreview(Board b)
    {
        preview.Text=string.Join(Environment.NewLine,Enumerable.Range(0,5).Select(r=>string.Join(" ",b.Cells.Skip(r*5).Take(5).Select(v=>v<0?"·":v==0?"X":v.ToString()))+$"   Σ{b.Rows[r].Sum:00} / X{b.Rows[r].Bombs}"))+Environment.NewLine+"열 Σ "+string.Join(" ",b.Columns.Select(h=>h.Sum.ToString("00")))+Environment.NewLine+"열 X "+string.Join("  ",b.Columns.Select(h=>h.Bombs));
    }
    void Render()
    {
        if(!running||!settings.HasRegion||current is null||result is null)return;
        var foreground=Native.GetForegroundWindow();if(!settings.ShowOverlayInCapture&&foreground!=target&&foreground!=Handle)return;
        var area=settings.Map(Native.ClientBounds(target));overlay.Render(area,current,result,settings);
        if(settings.ShowOverlayInCapture)obsCapture.Render(area,current,result,settings);else obsCapture.Clear();
        status.Text=result.Exact?"정확 계산 완료":"근삿값 표시 · 계산 종료";
        if(settings.ShowOverlayInCapture)status.Text+=" · 방송 캡처 표시";
    }
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();overlay.Dispose();obsCapture.Dispose();if(!busy)recognizer.Dispose();}base.Dispose(disposing);}
}
