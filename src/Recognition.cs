using System.Drawing.Drawing2D;

namespace VoltorbLens;
public sealed class Recognition : IDisposable
{
    readonly Bitmap[] tiles;
    readonly bool[][] digits;
    public Recognition(string assetPath)
    {
        tiles=new[]{"blank","1","2","3","voltorb"}.Select(n=>new Bitmap(Path.Combine(assetPath,$"tile_{n}.png"))).ToArray();
        digits=Enumerable.Range(0,10).Select(n=>{using var b=new Bitmap(Path.Combine(assetPath,$"number_bold_{n}.png"));return Enumerable.Range(0,48).Select(i=>b.GetPixel(i%6,i/6).A>128).ToArray();}).ToArray();
    }
    public Board Read(Bitmap source)
    {
        using var image=new Bitmap(192,192);
        using(var g=Graphics.FromImage(image)) { g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(source,new Rectangle(0,0,192,192),0,0,source.Width,source.Height,GraphicsUnit.Pixel); }
        var cells=new int[25];
        for(int r=0;r<5;r++)for(int c=0;c<5;c++) cells[r*5+c]=ReadTile(image,4+c*32,4+r*32);
        var rows=new Clue[5];var cols=new Clue[5];
        for(int i=0;i<5;i++)
        {
            rows[i]=new(ReadDigit(image,172,3+32*i)*10+ReadDigit(image,180,3+32*i),ReadDigit(image,180,16+32*i));
            cols[i]=new(ReadDigit(image,12+32*i,163)*10+ReadDigit(image,20+32*i,163),ReadDigit(image,20+32*i,176));
        }
        var board=new Board(rows,cols,cells);board.Validate();return board;
    }
    public Board ReadRegion(Bitmap source,Rectangle region)
    {
        var bounds=new Rectangle(Point.Empty,source.Size);
        if(!bounds.Contains(RequiredPixels(region)))throw new InvalidOperationException("카드 또는 힌트 숫자가 화면 밖에 있습니다.");
        // The virtual 192x192 grid extends beyond the game's bottom edge.
        // Only unused trailing padding may be absent; never pad a clue or card.
        if(bounds.Contains(region)){using var crop=source.Clone(region,System.Drawing.Imaging.PixelFormat.Format32bppArgb);return Read(crop);}
        using var padded=new Bitmap(region.Width,region.Height);
        using(var g=Graphics.FromImage(padded))
        {
            g.Clear(Color.Black);var visible=Rectangle.Intersect(bounds,region);
            g.DrawImage(source,new Rectangle(visible.X-region.X,visible.Y-region.Y,visible.Width,visible.Height),visible,GraphicsUnit.Pixel);
        }
        return Read(padded);
    }
    public static Rectangle RequiredPixels(Rectangle region)=>new(region.X,region.Y,(int)Math.Ceiling(region.Width*187.0/192),(int)Math.Ceiling(region.Height*185.0/192));
    // Locate the connected pale board grid anywhere in the emulator client.
    // Colour only proposes regions; all 25 cards and all clues must validate.
    public (Board Board,Rectangle Region) FindBoard(Bitmap source)
    {
        using var rgb=source.Clone(new Rectangle(Point.Empty,source.Size),System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var data=rgb.LockBits(new Rectangle(Point.Empty,rgb.Size),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var mask=new bool[rgb.Width*rgb.Height];
        try
        {
            var pixels=new byte[data.Stride*data.Height];System.Runtime.InteropServices.Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
            for(int y=0;y<rgb.Height;y++)for(int x=0;x<rgb.Width;x++)
            {
                int p=y*data.Stride+x*4;int b=pixels[p],g=pixels[p+1],r=pixels[p+2];
                mask[y*rgb.Width+x]=r>180&&g>200&&b>180&&g>r&&b>r;
            }
        }
        finally{rgb.UnlockBits(data);}
        var panels=new List<Rectangle>();var queue=new Queue<int>();
        for(int i=0;i<mask.Length;i++)
        {
            if(!mask[i])continue;mask[i]=false;queue.Enqueue(i);
            int left=i%rgb.Width,right=left,top=i/rgb.Width,bottom=top,count=0;
            while(queue.Count>0)
            {
                int p=queue.Dequeue(),x=p%rgb.Width,y=p/rgb.Width;count++;
                left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
                void Add(int n){if(mask[n]){mask[n]=false;queue.Enqueue(n);}}
                if(x>0)Add(p-1);if(x+1<rgb.Width)Add(p+1);if(y>0)Add(p-rgb.Width);if(y+1<rgb.Height)Add(p+rgb.Width);
            }
            int w=right-left+1,h=bottom-top+1;
            if(w>=120&&h>=120&&w/(double)h>.85&&w/(double)h<1.15&&count>w*h*.03)panels.Add(new(left,top,w,h));
        }
        foreach(var panel in panels.OrderByDescending(p=>p.Width*p.Height).Take(16))
        {

            double scale=panel.Width/188.0;int size=(int)Math.Round(192*scale);
            var expected=new Rectangle(panel.X-(int)Math.Round(2*scale),panel.Y-(int)Math.Round(scale),size,size);
            if(expected.Right<0||expected.Bottom<0||expected.X>=source.Width||expected.Y>=source.Height)continue;
            try{return(ReadRegion(source,expected),expected);}catch(InvalidOperationException){}catch(ArgumentException){}
            try{return Align(source,expected);}catch(InvalidOperationException){}catch(ArgumentException){}
        }
        throw new InvalidOperationException("보드를 찾는 중입니다. 찌리리공 뒤집기 화면 전체가 보이게 해 주세요.");
    }
    // Small registration search corrects drag offsets and menu/letterbox changes.
    // A candidate is accepted only when the complete board and all clues validate.
    public (Board Board,Rectangle Region) Align(Bitmap source,Rectangle expected)
    {
        var candidates=new List<(double Score,Rectangle Rect)>();
        foreach(double scale in new[]{.94,.97,1,1.03,1.06})
        for(int dy=-8;dy<=8;dy+=2)for(int dx=-8;dx<=8;dx+=2)
        {
            int w=(int)Math.Round(expected.Width*scale),h=(int)Math.Round(expected.Height*scale);
            var rect=new Rectangle(expected.X+(expected.Width-w)/2+(int)Math.Round(dx*expected.Width/192.0),expected.Y+(expected.Height-h)/2+(int)Math.Round(dy*expected.Height/192.0),w,h);
            if(!new Rectangle(Point.Empty,source.Size).Contains(RequiredPixels(rect)))continue;
            double score=0;
            foreach(int cell in new[]{0,4,12,20,24})
            {
                double best=double.MaxValue;
                foreach(var tile in tiles)
                {
                    double err=0;
                    for(int y=1;y<22;y+=4)for(int x=1;x<22;x+=4)
                    {
                        int px=rect.X+(int)((4+cell%5*32+x+.5)*w/192.0),py=rect.Y+(int)((4+cell/5*32+y+.5)*h/192.0);
                        var a=source.GetPixel(px,py);var b=tile.GetPixel(x,y);err+=Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B);
                    }
                    best=Math.Min(best,err);
                }
                score+=best;
            }
            candidates.Add((score,rect));
        }
        foreach(var item in candidates.OrderBy(x=>x.Score).Take(8))
            try{return(ReadRegion(source,item.Rect),item.Rect);}catch(InvalidOperationException){}catch(ArgumentException){}
        throw new InvalidOperationException("보드 위치를 확인하지 못했습니다. 영역을 다시 지정하세요.");
    }
    int ReadDigit(Bitmap image,int x,int y)
    {
        var scores=new double[10]; Array.Fill(scores,1);
        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
        {
            var mask=new bool[48];
            for(int k=0;k<48;k++){var p=image.GetPixel(x+dx+k%6,y+dy+k/6);mask[k]=Math.Max(p.R,Math.Max(p.G,p.B))<135 && Math.Max(p.R,Math.Max(p.G,p.B))-Math.Min(p.R,Math.Min(p.G,p.B))<65;}
            for(int d=0;d<10;d++) scores[d]=Math.Min(scores[d],Enumerable.Range(0,48).Count(k=>mask[k]!=digits[d][k])/48.0);
        }
        var rank=Enumerable.Range(0,10).OrderBy(d=>scores[d]).ToArray();
        if(scores[rank[0]]>.16 || scores[rank[1]]-scores[rank[0]]<.035)throw new InvalidOperationException("힌트 숫자 인식 대기 · 영역/화면 배율을 확인하세요.");
        return rank[0];
    }
    int ReadTile(Bitmap image,int x,int y)
    {
        var scores=new double[tiles.Length];Array.Fill(scores,1);
        for(int t=0;t<tiles.Length;t++)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
        {
            double error=0,weightSum=0;
            for(int py=0;py<22;py++)for(int px=0;px<22;px++)
            {
                var a=image.GetPixel(x+dx+px,y+dy+py);var b=tiles[t].GetPixel(px,py);
                double weight=px>=6&&px<16&&py>=5&&py<17?4:1;
                error+=weight*(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B))/(255.0*3);weightSum+=weight;
            }
            scores[t]=Math.Min(scores[t],error/weightSum);
        }
        var rank=Enumerable.Range(0,tiles.Length).OrderBy(i=>scores[i]).ToArray();
        if(scores[rank[0]]>.105 || scores[rank[1]]-scores[rank[0]]<.012)throw new InvalidOperationException($"카드 인식 대기 · 애니메이션/메모/가림 여부를 확인하세요. ({x},{y}: {scores[rank[0]]:0.000}/{scores[rank[1]]:0.000})");
        return rank[0] switch {0=>-1,4=>0,_=>rank[0]};
    }
    public void Dispose(){foreach(var t in tiles)t.Dispose();}
}

public sealed class RoundTracker
{
    public Board? Last {get;private set;}
    public bool AwaitingStart {get;private set;}
    public void Reset(){Last=null;AwaitingStart=false;}
    public bool Accept(Board board,bool autoRound)
    {
        if(AwaitingStart && !autoRound)return false;
        if(Last is not null && board.IsNewRoundFrom(Last) && !autoRound){AwaitingStart=true;return false;}
        Last=board;
        if(board.Finished){AwaitingStart=!autoRound;return false;}
        AwaitingStart=false;return true;
    }
}


