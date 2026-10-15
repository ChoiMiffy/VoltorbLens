using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

internal static class Launcher
{
    [STAThread]
    private static void Main()
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app");
        string executable = Path.Combine(folder, "VoltorbLens.exe");
        try
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("Keep the app folder beside this launcher.");
            Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = folder, UseShellExecute = true });
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "Voltorb Lens", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
