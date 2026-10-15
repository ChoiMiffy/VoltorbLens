using System.Text.Json;

namespace VoltorbLens;
public sealed class Settings
{
    public string ProcessName {get;set;}="";
    public int ClientWidth {get;set;}
    public int ClientHeight {get;set;}
    public int X {get;set;}
    public int Y {get;set;}
    public int Width {get;set;}
    public int Height {get;set;}
    public bool AutoRound {get;set;}=true;
    public bool AlwaysTop {get;set;}
    public bool ShowRecognition {get;set;}
    public int TextAlpha {get;set;}=100;
    public int BackgroundAlpha {get;set;}=75;
    public bool HasRegion => ClientWidth>0 && ClientHeight>0 && Width>0 && Height>0;
    public static string FilePath => Path.Combine(AppContext.BaseDirectory,"settings.json");
    public static Settings Load() { try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath))??new(); } catch { return new(); } }
    public void Save() { var temp=FilePath+".tmp"; File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true})); File.Move(temp,FilePath,true); }
    public Rectangle Map(Rectangle client)
    {
        double scale=Math.Min((double)client.Width/ClientWidth,(double)client.Height/ClientHeight);
        double ox=(client.Width-ClientWidth*scale)/2, oy=(client.Height-ClientHeight*scale)/2;
        return new(client.X+(int)Math.Round(ox+X*scale),client.Y+(int)Math.Round(oy+Y*scale),(int)Math.Round(Width*scale),(int)Math.Round(Height*scale));
    }
}
