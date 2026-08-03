namespace WheelSimuServer;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败:\n{ex}", "WheelSimu Server Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
