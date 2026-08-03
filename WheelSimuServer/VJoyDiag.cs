using System.Runtime.InteropServices;

namespace WheelSimuServer;

static class VJoyDiag
{
    const string DLL = "vJoyInterface.dll";
    const uint DEV_ID = 1;

    [DllImport(DLL)] static extern bool vJoyEnabled();
    [DllImport(DLL)] static extern int  GetVJDStatus(uint rID);
    [DllImport(DLL)] static extern bool AcquireVJD(uint rID);
    [DllImport(DLL)] static extern void RelinquishVJD(uint rID);
    [DllImport(DLL)] static extern int  GetVJDButtonNumber(uint rID);
    [DllImport(DLL)] static extern int  GetVJDDiscPovNumber(uint rID);
    [DllImport(DLL)] static extern int  GetVJDContPovNumber(uint rID);
    [DllImport(DLL)] static extern bool GetVJDAxisExist(uint rID, int Axis);
    [DllImport(DLL)] static extern int  GetVJDAxisMax(uint rID, int Axis);
    [DllImport(DLL)] static extern int  GetVJDAxisMin(uint rID, int Axis);
    [DllImport(DLL)] static extern bool isVJDExists(uint rID);

    enum HID_USAGES : int
    {
        HID_USAGE_X      = 0x30,
        HID_USAGE_Y      = 0x31,
        HID_USAGE_Z      = 0x32,
        HID_USAGE_RX     = 0x33,
        HID_USAGE_RY     = 0x34,
        HID_USAGE_RZ     = 0x35,
        HID_USAGE_SL0    = 0x36,
        HID_USAGE_SL1    = 0x37,
        HID_USAGE_WHL    = 0x38,
    }

    static string StatusName(int s) => s switch
    {
        0 => "FREE",
        1 => "BUSY",
        2 => "MISS",
        3 => "OWN",
        _ => $"UNKN({s})"
    };

    public static void Run(Action<string>? writeLine = null)
    {
        void W(string s) { (writeLine ?? Console.WriteLine)(s); }

        W("=== vJoy 诊断 ===");
        W("");

        bool enabled = vJoyEnabled();
        W($"vJoyEnabled()      = {enabled}");

        if (!enabled)
        {
            W("[!] vJoy 驱动未启用!");
            return;
        }

        bool exists = isVJDExists(DEV_ID);
        W($"isVJDExists(1)     = {exists}");

        if (!exists)
        {
            W("[!] vJoy 设备 1 不存在! 请用 vJoyConf 配置设备 1");
            return;
        }

        int status = GetVJDStatus(DEV_ID);
        W($"GetVJDStatus(1)    = {StatusName(status)}");

        W("");
        W("--- 设备 1 轴配置 ---");
        int[] axes = { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 };
        string[] names = { "X", "Y", "Z", "RX", "RY", "RZ", "SL0", "SL1", "WHL" };
        int axisCount = 0;
        for (int i = 0; i < axes.Length; i++)
        {
            bool axisExists = GetVJDAxisExist(DEV_ID, axes[i]);
            if (axisExists)
            {
                int min = GetVJDAxisMin(DEV_ID, axes[i]);
                int max = GetVJDAxisMax(DEV_ID, axes[i]);
                W($"  {names[i]}  : OK [min={min}, max={max}]");
                axisCount++;
            }
            else
            {
                W($"  {names[i]}  : 未启用");
            }
        }
        W($"  已启用轴数: {axisCount}");

        int buttons = GetVJDButtonNumber(DEV_ID);
        int contPov = GetVJDContPovNumber(DEV_ID);
        int discPov = GetVJDDiscPovNumber(DEV_ID);
        W($"  按钮数: {buttons}, Continuous POV: {contPov}, Discrete POV: {discPov}");

        W("");
        W("--- 尝试获取设备 ---");
        if (status == 3)
        {
            RelinquishVJD(DEV_ID);
            W("  释放旧占用...");
            System.Threading.Thread.Sleep(200);
            status = GetVJDStatus(DEV_ID);
            W($"  新状态: {StatusName(status)}");
        }

        if (status == 0 || status == 1)
        {
            bool acquired = AcquireVJD(DEV_ID);
            W($"  AcquireVJD(1) = {acquired}");
            if (acquired)
            {
                W("  成功获取设备!");
                RelinquishVJD(DEV_ID);
            }
            else
            {
                W("  获取失败!");
            }
        }
    }
}
