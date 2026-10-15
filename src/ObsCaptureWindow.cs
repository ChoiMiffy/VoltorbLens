using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace VoltorbLens;

/// <summary>Opaque, independently capturable feed for OBS. Chroma-key the magenta background.</summary>
public sealed class ObsCaptureWindow : Form
{
    static readonly Color KeyColor=Color.Magenta;
    Board? board;
    SolveResult? result;

    public ObsCaptureWindow()
    {
        Text="VoltorbLens OBS Feed";
        FormBorderStyle=FormBorderStyle.None;
        ShowInTaskbar=true;
        TopMost=false;
        StartPosition=FormStartPosition.Manual;
        BackColor=KeyColor;
        DoubleBuffered=true;
    }

    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams
    {
        get
        {
            var p=base.CreateParams;
            p.ExStyle=(p.ExStyle&~0x80)|0x40000|0x08000000;
            return p;
        }
    }

    public void Render(Rectangle area,Board nextBoard,SolveResult nextResult,Settings settings)
    {
        if(area.Width<48||area.Height<48)return;
        Bounds=area;
        board=nextBoard;
        result=nextResult;
        BackgroundAlpha=settings.BackgroundAlpha;
        TextAlpha=settings.TextAlpha;
        if(!Visible)Show();
        Native.SetWindowPos(Handle,1,0,0,0,0,0x0001|0x0002|0x0010|0x0040);
        Invalidate();
    }

    public void Clear()
    {
        board=null;
        result=null;
        if(!IsHandleCreated)return;
        if(!Visible)Show();
        Native.SetWindowPos(Handle,1,0,0,0,0,0x0001|0x0002|0x0010|0x0040);
        Invalidate();
    }

    int BackgroundAlpha { get; set; }=75;
    int TextAlpha { get; set; }=100;

    static Color Composite(Color color,int alpha)
    {
        double a=Math.Clamp(alpha,0,100)/100d;
        return Color.FromArgb(
            (int)Math.Round(color.R*a+41*(1-a)),
            (int)Math.Round(color.G*a+165*(1-a)),
            (int)Math.Round(color.B*a+107*(1-a)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(KeyColor);
        if(board is null||result is null)return;
        e.Graphics.SmoothingMode=SmoothingMode.None;
        e.Graphics.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        float cell=ClientSize.Width/6f,height=ClientSize.Height/6f;
        using var big=new Font("Malgun Gothic",Math.Max(4,cell*.135f),FontStyle.Bold,GraphicsUnit.Pixel);
        using var small=new Font("Malgun Gothic",Math.Max(3,cell*.112f),FontStyle.Regular,GraphicsUnit.Pixel);
        using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter};
        for(int i=0;i<25;i++)
        {
            if(board.Cells[i]>=0)continue;
            var rect=new RectangleF(i%5*cell+cell*.025f,i/5*height+height*.15f,cell*.95f,height*.65f);
            using var bg=new SolidBrush(Composite(Color.FromArgb(12,18,28),BackgroundAlpha));
            e.Graphics.FillRectangle(bg,rect);
            Color source=result.Exact&&result.Bomb[i]==0?Color.FromArgb(100,255,155):result.Exact&&result.Bomb[i]==1?Color.FromArgb(255,108,108):Color.White;
            using var fg=new SolidBrush(Composite(source,TextAlpha));
            e.Graphics.DrawString("위험 "+ProbabilityText.Format(result.Bomb[i],result.Exact),big,fg,new RectangleF(rect.X,rect.Y,rect.Width,rect.Height*.53f),format);
            e.Graphics.DrawString("2·3 "+ProbabilityText.Format(result.Reward[i],result.Exact),small,fg,new RectangleF(rect.X,rect.Y+rect.Height*.5f,rect.Width,rect.Height*.45f),format);
        }
    }
}
