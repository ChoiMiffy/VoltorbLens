using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

internal static class Launcher
{
    [STAThread]
    private static void Main()
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "프로그램");
        string executable = Path.Combine(folder, "VoltorbLens.exe");
        try
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("프로그램 폴더를 실행 파일과 함께 보관해 주세요.");
            Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = folder, UseShellExecute = true });
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "찌리리공 렌즈", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
