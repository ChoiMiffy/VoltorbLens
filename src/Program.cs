namespace VoltorbLens;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Contains("--self-test")){Environment.Exit(SelfTests.Run());return;}
        try { Application.Run(new MainForm()); }
        catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"error.log"),e.ToString());MessageBox.Show(e.Message,"찌리리공 렌즈");}
    }
}
