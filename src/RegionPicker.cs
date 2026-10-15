namespace VoltorbLens;
public sealed class RegionPicker : Form
{
    readonly Bitmap shot; Point anchor; Rectangle selection; bool dragging;
    public Rectangle Selected => selection;
    public RegionPicker(Rectangle client)
    {
        shot=Native.Capture(client); Bounds=client; StartPosition=FormStartPosition.Manual; FormBorderStyle=FormBorderStyle.None;
        TopMost=true; DoubleBuffered=true; Cursor=Cursors.Cross; KeyPreview=true; ShowInTaskbar=false;
        KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}};
        MouseDown+=(_,e)=>{anchor=e.Location;dragging=true;};
        MouseMove+=(_,e)=>{if(!dragging)return;selection=Rectangle.FromLTRB(Math.Min(anchor.X,e.X),Math.Min(anchor.Y,e.Y),Math.Max(anchor.X,e.X),Math.Max(anchor.Y,e.Y));Invalidate();};
        MouseUp+=(_,e)=>{dragging=false;if(selection.Width>=96&&selection.Height>=96){DialogResult=DialogResult.OK;Close();}};
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(shot,0,0);
        using var shade=new SolidBrush(Color.FromArgb(90,0,0,0));e.Graphics.FillRectangle(shade,ClientRectangle);
        if(selection.Width>0){e.Graphics.DrawImage(shot,selection,selection,GraphicsUnit.Pixel);using var pen=new Pen(Color.Cyan,2);e.Graphics.DrawRectangle(pen,selection);
            for(int k=1;k<6;k++){e.Graphics.DrawLine(Pens.Cyan,selection.X+selection.Width*k/6,selection.Y,selection.X+selection.Width*k/6,selection.Bottom);e.Graphics.DrawLine(Pens.Cyan,selection.X,selection.Y+selection.Height*k/6,selection.Right,selection.Y+selection.Height*k/6);}}
        using var font=new Font("Malgun Gothic",12,FontStyle.Bold); var text="첫 카드의 바깥 왼쪽 위부터 마지막 힌트의 바깥 오른쪽 아래까지 드래그\n카드 5×5 + 오른쪽·아래 힌트 = 6×6 정사각형 · Esc 취소";
        e.Graphics.FillRectangle(Brushes.Black,0,0,Width,64);e.Graphics.DrawString(text,font,Brushes.White,new RectangleF(8,4,Width-16,60));
    }
    protected override void Dispose(bool disposing){if(disposing)shot.Dispose();base.Dispose(disposing);}
}
