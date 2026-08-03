using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WheelSimuServer;

public partial class MainForm : Form
{
    // ==================== vJoy P/Invoke ====================
    const int VJD_STAT_FREE = 0;
    const int VJD_STAT_OWN = 3;
    const int VJOY_AXIS_MAX = 32768;
    const uint VJOY_DEVICE_ID = 1;

    [StructLayout(LayoutKind.Sequential)]
    struct JState
    {
        public byte bDevice;
        public int wThrottle, wRudder, wAileron, wAxisX, wAxisY, wAxisZ;
        public int wAxisXRot, wAxisYRot, wAxisZRot, wSlider, wDial, wWheel;
        public int wAxisVX, wAxisVY, wAxisVZ, wAxisVBRX, wAxisVBRY, wAxisVBRZ;
        public int lButtons;
        public uint bHats, bHatsEx1, bHatsEx2, bHatsEx3;
    }

    [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern bool vJoyEnabled();
    [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int GetVJDStatus(uint rID);
    [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern bool AcquireVJD(uint rID);
    [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void RelinquishVJD(uint rID);
    [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern bool UpdateVJD(uint rID, ref JState pData);
    [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern bool ResetVJD(uint rID);

    // ==================== 配置 ====================
    const int LISTEN_PORT = 5050;
    const int DISCOVERY_PORT = 5051;
    const string DISCOVERY_MAGIC = "WHEELSIMU_SERVER";
    const int MAX_LOG_LINES = 1000;
    const int SMOOTH_STEP = VJOY_AXIS_MAX / 30;

    // ==================== 状态 ====================
    volatile bool vJoyReady;
    readonly object vJoyLock = new();
    CancellationTokenSource? _cts;
    bool _isExiting;

    // vJoy 平滑状态
    double lastAngle;
    int lastThrottle, lastBrake, lastClutch, lastAxisXRot;

    // 统计
    int msgCount;
    int clientCount;
    DateTime lastDataLog = DateTime.MinValue;

    // ==================== UI 控件 ====================
    RichTextBox rtbLogs = null!;
    StatusStrip statusBar = null!;
    ToolStripStatusLabel lblVJoy = null!;
    ToolStripStatusLabel lblIP = null!;
    ToolStripStatusLabel lblClient = null!;
    ToolStripStatusLabel lblMsgCount = null!;
    NotifyIcon trayIcon = null!;

    // ==================== 构造函数 ====================
    public MainForm(string[] args)
    {
        InitializeComponent();
        SetupTray();

        // --tray 参数：启动后自动隐藏
        if (args.Length > 0 && args[0] == "--tray")
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }
    }

    void InitializeComponent()
    {
        Text = "WheelSimu Server v2";
        Size = new Size(650, 430);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = true;
        MaximizeBox = false;
        Icon = CreateAppIcon();

        // === 顶部标题栏 ===
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(45, 45, 48)
        };
        var lblTitle = new Label
        {
            Text = "  WheelSimu Server v2 — 方向盘手机模拟器  ",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(12, 9)
        };
        pnlTop.Controls.Add(lblTitle);
        Controls.Add(pnlTop);

        // === 日志区域 ===
        rtbLogs = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            WordWrap = true,
            DetectUrls = false
        };
        Controls.Add(rtbLogs);

        // === 底部状态栏 ===
        statusBar = new StatusStrip
        {
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.FromArgb(200, 200, 200),
            SizingGrip = false
        };
        lblVJoy = new ToolStripStatusLabel { Text = "vJoy: 检测中...", Padding = new Padding(6, 0, 12, 0) };
        lblIP = new ToolStripStatusLabel { Text = "IP: ---", Padding = new Padding(0, 0, 12, 0) };
        lblClient = new ToolStripStatusLabel { Text = "客户端: 0", Padding = new Padding(0, 0, 12, 0) };
        lblMsgCount = new ToolStripStatusLabel { Text = "消息: 0" };
        statusBar.Items.Add(lblVJoy);
        statusBar.Items.Add(lblIP);
        statusBar.Items.Add(lblClient);
        statusBar.Items.Add(lblMsgCount);
        Controls.Add(statusBar);

        // 事件
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        Resize += MainForm_Resize;
    }

    // ==================== 图标生成 ====================
    static Icon CreateAppIcon()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.FromArgb(0, 120, 212));
        g.FillEllipse(brush, 2, 2, 28, 28);
        using var white = new SolidBrush(Color.White);
        g.FillEllipse(white, 10, 10, 12, 12);
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ==================== 系统托盘 ====================
    void SetupTray()
    {
        var menu = new ContextMenuStrip();

        var titleItem = new ToolStripMenuItem("WheelSimu Server v2")
        {
            Font = new Font(menu.Font, FontStyle.Bold),
            Enabled = false
        };
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());

        var showItem = new ToolStripMenuItem("显示窗口", null, (_, _) => ShowWindow());
        menu.Items.Add(showItem);

        var hideItem = new ToolStripMenuItem("隐藏到托盘", null, (_, _) => HideToTray());
        menu.Items.Add(hideItem);
        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出程序", null, (_, _) =>
        {
            _isExiting = true;
            trayIcon.Visible = false;
            Application.Exit();
        });
        menu.Items.Add(exitItem);

        trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "WheelSimu Server",
            ContextMenuStrip = menu,
            Visible = true
        };
        trayIcon.MouseDoubleClick += (_, _) => ShowWindow();
    }

    void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        Activate();
    }

    void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    // ==================== 窗口事件 ====================
    void MainForm_Load(object? sender, EventArgs e)
    {
        Log("=================================");
        Log("  WheelSimu Server v2");
        Log("  方向盘手机模拟器 - PC 服务端");
        Log("=================================");
        Log("");

        // vJoy 诊断
        try
        {
            VJoyDiag.Run(s => Log($"  {s}"));
        }
        catch (Exception ex)
        {
            Log($"vJoy 诊断异常: {ex.Message}");
        }

        Log("");

        // 初始化 vJoy
        try
        {
            vJoyReady = InitVJoy();
        }
        catch (Exception ex)
        {
            Log($"vJoy 异常: {ex.Message}");
        }

        if (vJoyReady)
        {
            UpdateStatusUI("vJoy: OK", "vJoy 就绪");
        }
        else
        {
            Log("vJoy 未就绪，仅转发数据，不输出虚拟手柄");
            UpdateStatusUI("vJoy: OFF", "vJoy 未就绪");
        }

        string ip = GetLocalIP();
        Log($"本机 IP: {ip}");
        Log($"监听端口: {LISTEN_PORT}");
        UpdateStatusUI(null, null, $"IP: {ip}:{LISTEN_PORT}");

        // 启动服务器
        _cts = new CancellationTokenSource();
        _ = RunServer(_cts.Token);

        if (ShowInTaskbar)
        {
            Log("关闭窗口将最小化到托盘，右键托盘图标可退出");
        }
        Log("");
    }

    void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_isExiting)
        {
            // 关闭窗口 → 隐藏到托盘
            e.Cancel = true;
            HideToTray();
            return;
        }

        // 真正退出
        _cts?.Cancel();
        if (vJoyReady)
        {
            try
            {
                JState zero = new() { bDevice = (byte)VJOY_DEVICE_ID };
                UpdateVJD(VJOY_DEVICE_ID, ref zero);
                RelinquishVJD(VJOY_DEVICE_ID);
                Log("vJoy 设备已释放");
            }
            catch { }
        }
        trayIcon.Visible = false;
        trayIcon.Dispose();
    }

    void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray();
        }
    }

    // ==================== 安全 UI 更新 ====================
    void Log(string msg)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Log(msg));
            return;
        }

        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        rtbLogs.AppendText(line + "\n");

        // 限制行数
        if (rtbLogs.Lines.Length > MAX_LOG_LINES)
        {
            int excess = rtbLogs.Lines.Length - MAX_LOG_LINES;
            int pos = 0;
            for (int i = 0; i < excess; i++)
                pos = rtbLogs.Text.IndexOf('\n', pos) + 1;
            if (pos > 0) rtbLogs.Select(0, pos);
            rtbLogs.SelectedText = "";
        }

        rtbLogs.SelectionStart = rtbLogs.TextLength;
        rtbLogs.ScrollToCaret();
    }

    void UpdateStatusUI(string? vJoyText = null, string? trayText = null, string? ipText = null)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateStatusUI(vJoyText, trayText, ipText));
            return;
        }

        if (vJoyText != null) lblVJoy.Text = vJoyText;
        if (ipText != null) lblIP.Text = ipText;
        if (trayText != null) trayIcon.Text = "WheelSimu Server - " + trayText;

        lblClient.Text = $"客户端: {clientCount}";
        lblMsgCount.Text = $"消息: {msgCount}";
    }

    // ==================== vJoy 初始化 ====================
    bool InitVJoy()
    {
        try
        {
            if (!vJoyEnabled())
            {
                Log("vJoy 驱动未启用，请安装并配置 vJoy");
                return false;
            }
            Log("vJoy 驱动已检测");

            int status = GetVJDStatus(VJOY_DEVICE_ID);
            Log($"  设备 {VJOY_DEVICE_ID} 状态: {(status == 0 ? "空闲" : status == 1 ? "占用" : status == 2 ? "不存在" : status == 3 ? "本进程" : "未知" + status)}");

            if (status == VJD_STAT_OWN)
            {
                RelinquishVJD(VJOY_DEVICE_ID);
                Thread.Sleep(200);
            }

            if (!AcquireVJD(VJOY_DEVICE_ID))
            {
                Log($"获取设备 {VJOY_DEVICE_ID} 失败 (可能被其他程序占用)");
                return false;
            }

            JState initState = new() { bDevice = (byte)VJOY_DEVICE_ID };
            UpdateVJD(VJOY_DEVICE_ID, ref initState);
            Log($"vJoy 设备 {VJOY_DEVICE_ID} 就绪!");
            return true;
        }
        catch (Exception ex)
        {
            Log($"vJoy 初始化异常: {ex.Message}");
            return false;
        }
    }

    // ==================== UDP 广播发现 ====================
    async Task BroadcastDiscovery(CancellationToken ct)
    {
        string localIp = GetLocalIP();
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        var endpoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);
        var payload = $"{DISCOVERY_MAGIC}:{localIp}:{LISTEN_PORT}";
        var data = Encoding.UTF8.GetBytes(payload);

        while (!ct.IsCancellationRequested)
        {
            try { await udp.SendAsync(data, data.Length, endpoint); }
            catch { }
            try { await Task.Delay(2000, ct); } catch { break; }
        }
    }

    // ==================== TCP 服务器 ====================
    async Task RunServer(CancellationToken ct)
    {
        _ = BroadcastDiscovery(ct);

        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Any, LISTEN_PORT);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            Log($"TCP 服务已启动 -> 0.0.0.0:{LISTEN_PORT} (等待手机连接...)");
            Log($"UDP 广播发现 -> 端口 {DISCOVERY_PORT}");

            while (!ct.IsCancellationRequested)
            {
                var acceptTask = listener.AcceptTcpClientAsync(ct);
                try { await acceptTask; }
                catch (OperationCanceledException) { break; }

                Interlocked.Increment(ref clientCount);
                UpdateStatusUI();
                _ = HandleClient(acceptTask.Result, ct);
            }
        }
        finally
        {
            listener?.Stop();
            Log("TCP 服务已停止");
        }
    }

    // ==================== 客户端处理 ====================
    async Task HandleClient(TcpClient client, CancellationToken ct)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "?";
        Log($"[连接] {remote}");

        try
        {
            using var stream = client.GetStream();
            stream.ReadTimeout = 3000;
            var buffer = new byte[512];
            var leftover = "";

            while (!ct.IsCancellationRequested && client.Connected)
            {
                int count;
                try
                {
                    count = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (count == 0) break;
                }
                catch (IOException) { break; }
                catch (OperationCanceledException) { break; }

                string raw = Encoding.UTF8.GetString(buffer, 0, count);
                leftover += raw;

                while (true)
                {
                    int atIdx = leftover.IndexOf('@');
                    if (atIdx < 0) break;

                    string msg = leftover.Substring(0, atIdx);
                    leftover = leftover.Substring(atIdx + 1);
                    ProcessMessage(msg);

                    string ack = "OK:FF=0,CC=0@";
                    byte[] ackBytes = Encoding.UTF8.GetBytes(ack);
                    try { await stream.WriteAsync(ackBytes, 0, ackBytes.Length, ct); }
                    catch { break; }
                }

                if (leftover.Length > 4096) leftover = "";
            }
        }
        catch (Exception ex)
        {
            Log($"[错误] {remote}: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref clientCount);
            UpdateStatusUI();
            Log($"[断开] {remote}");
        }
    }

    // ==================== 消息解析 ====================
    void ProcessMessage(string msg)
    {
        Interlocked.Increment(ref msgCount);

        double angle = 0;
        int throttle = 0, brake = 0, clutch = 0, handbrake = 0;
        int gearUp = 0, gearDown = 0;

        var parts = msg.Split(',');
        foreach (var part in parts)
        {
            int eqIdx = part.IndexOf('=');
            if (eqIdx < 0) continue;
            string key = part.Substring(0, eqIdx);
            string valStr = part.Substring(eqIdx + 1);

            switch (key)
            {
                case "A": double.TryParse(valStr, out angle); break;
                case "T": int.TryParse(valStr, out throttle); break;
                case "B": int.TryParse(valStr, out brake); break;
                case "C": int.TryParse(valStr, out clutch); break;
                case "H": int.TryParse(valStr, out handbrake); break;
                case "Gu": int.TryParse(valStr, out gearUp); break;
                case "Gd": int.TryParse(valStr, out gearDown); break;
            }
        }

        var now = DateTime.Now;
        if ((now - lastDataLog).TotalSeconds >= 1.0)
        {
            lastDataLog = now;
            string tag = vJoyReady ? "vJoy" : "收";
            Log($"[{tag} #{msgCount}] A={angle:F1} T={throttle} B={brake} C={clutch} HB={handbrake} Gu={gearUp} Gd={gearDown}");
            UpdateStatusUI();
        }

        if (vJoyReady)
            UpdateVJoy(angle, throttle, brake, clutch, handbrake, gearUp, gearDown);
    }

    // ==================== 更新 vJoy ====================
    static int Smooth(int current, int target, int step)
    {
        if (current < target) return Math.Min(current + step, target);
        if (current > target) return Math.Max(current - step, target);
        return target;
    }

    void UpdateVJoy(double angle, int throttle, int brake, int clutch,
                    int handbrake, int gearUp, int gearDown)
    {
        lock (vJoyLock)
        {
            if (Math.Abs(angle - lastAngle) < 0.3) angle = lastAngle;
            lastAngle = angle;

            double ratio = (double)VJOY_AXIS_MAX / 900.0;
            int axisX = (int)Math.Round(16384 + angle * ratio);
            axisX = Math.Clamp(axisX, -32768, 32767);

            int targetThrottle = (int)((long)throttle * VJOY_AXIS_MAX / 100);
            int targetBrake = (int)((long)brake * VJOY_AXIS_MAX / 100);
            int targetClutch = (int)((long)clutch * VJOY_AXIS_MAX / 100);

            if (targetThrottle > 0) targetBrake = 0;
            if (targetBrake > 0) targetThrottle = 0;

            lastThrottle = Smooth(lastThrottle, targetThrottle, SMOOTH_STEP);
            lastBrake = Smooth(lastBrake, targetBrake, SMOOTH_STEP);
            lastClutch = Smooth(lastClutch, targetClutch, SMOOTH_STEP);

            int axisY = lastThrottle;
            int axisZ = lastBrake;
            int axisYRot = lastClutch;

            int targetHB = handbrake > 0 ? VJOY_AXIS_MAX : 0;
            lastAxisXRot = Smooth(lastAxisXRot, targetHB, VJOY_AXIS_MAX / 4);

            int buttons = 0;
            if (gearUp > 0) buttons |= 1;
            if (gearDown > 0) buttons |= 2;
            if (handbrake > 0) buttons |= 4;

            JState state = new()
            {
                bDevice = (byte)VJOY_DEVICE_ID,
                wAxisX = axisX,
                wAxisY = axisY,
                wAxisZ = axisZ,
                wAxisXRot = lastAxisXRot,
                wAxisYRot = axisYRot,
                lButtons = buttons,
            };

            UpdateVJD(VJOY_DEVICE_ID, ref state);
        }
    }

    // ==================== 辅助 ====================
    static string GetLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork
                                  && !a.ToString().StartsWith("127."));
            return ip?.ToString() ?? "?.?.?.?";
        }
        catch { return "?.?.?.?"; }
    }
}
