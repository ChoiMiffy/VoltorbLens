using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace VoltorbLens;
public sealed class Overlay : Form
{
    public bool CaptureExcluded {get;private set;}
    public Overlay() { FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; StartPosition=FormStartPosition.Manual; }
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle|=0x80000|0x20|0x80|0x08000000; return p; } }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); CaptureExcluded=Environment.OSVersion.Version.Build>=19041 && Native.SetWindowDisplayAffinity(Handle,0x11); }
    protected override void WndProc(ref Message m) { if(m.Msg==0x84) {m.Result=-1;return;} if(m.Msg==0x21){m.Result=3;return;} base.WndProc(ref m); }
    public void Render(Rectangle area, Board board, SolveResult result, Settings settings)
    {
        if(area.Width<48||area.Height<48)return;
        Bounds=area;
        if(!Visible)Show();
        if(!CaptureExcluded && Environment.OSVersion.Version.Build>=19041) CaptureExcluded=Native.SetWindowDisplayAffinity(Handle,0x11);
        using var image=new Bitmap(area.Width,area.Height,PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(image);
        g.CompositingMode=CompositingMode.SourceOver; g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        float cell=area.Width/6f, height=area.Height/6f;
        using var big=new Font("Malgun Gothic",Math.Max(4,cell*.135f),FontStyle.Bold,GraphicsUnit.Pixel);
        using var small=new Font("Malgun Gothic",Math.Max(3,cell*.112f),FontStyle.Regular,GraphicsUnit.Pixel);
        using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
        for(int i=0;i<25;i++)
        {
            if(board.Cells[i]>=0)continue;
            var rect=new RectangleF(i%5*cell+cell*.025f,i/5*height+height*.15f,cell*.95f,height*.65f);
            using var bg=new SolidBrush(Color.FromArgb(settings.BackgroundAlpha*255/100,12,18,28)); g.FillRectangle(bg,rect);
            var color=result.Exact && result.Bomb[i]==0 ? Color.FromArgb(100,255,155) : result.Exact&&result.Bomb[i]==1 ? Color.FromArgb(255,108,108):Color.White;
            using var fg=new SolidBrush(Color.FromArgb(settings.TextAlpha*255/100,color));
            g.DrawString("위험 "+ProbabilityText.Format(result.Bomb[i],result.Exact),big,fg,new RectangleF(rect.X,rect.Y,rect.Width,rect.Height*.53f),format);
            g.DrawString("2·3 "+ProbabilityText.Format(result.Reward[i],result.Exact),small,fg,new RectangleF(rect.X,rect.Y+rect.Height*.5f,rect.Width,rect.Height*.45f),format);
        }
        Native.Layer(Handle,image,area.Location);
    }
}
