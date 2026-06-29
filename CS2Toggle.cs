using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// ============================================================================
//  NvidiaCS2Toggle
//  App de bandeja del sistema para alternar entre un perfil "CS2" y "Normal".
//   - Cambia la resolucion de pantalla (Windows ChangeDisplaySettingsEx).
//   - Cambia el Digital Vibrance de NVIDIA (NVAPI).
//   - Los cambios se aplican SOLO al monitor seleccionado como "Monitor de juego".
//
//  Perfiles configurables abajo en la seccion CONFIG.
// ============================================================================

namespace NvidiaCS2Toggle
{
    // -----------------------------------------------------------------------
    //  Info de un monitor
    // -----------------------------------------------------------------------
    class MonitorInfo
    {
        public string Device;    // \\.\DISPLAY1
        public string Friendly;  // nombre del monitor (modelo)
        public bool   Primary;
    }

    // -----------------------------------------------------------------------
    //  Config persistente: archivo key=value junto al .exe (config.txt).
    //  ponytail: formato key=value, no JSON; nada que parsear vale una dependencia.
    // -----------------------------------------------------------------------
    static class Settings
    {
        static string File_
        {
            get { return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath), "config.txt"); }
        }

        static readonly Dictionary<string, string> D = Load();

        static Dictionary<string, string> Load()
        {
            var m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var line in File.ReadAllLines(File_))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0) m[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
            catch { }
            return m;
        }

        public static void Save()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in D) lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllText(File_, string.Join(Environment.NewLine, lines.ToArray()));
            }
            catch { }
        }

        static int GetI(string k, int def)
        {
            string v; int n;
            return (D.TryGetValue(k, out v) && int.TryParse(v, out n)) ? n : def;
        }
        static void SetI(string k, int v) { D[k] = v.ToString(); }

        public static string GameMonitor
        {
            get { string v; return D.TryGetValue("monitor", out v) ? v : ""; }
            set { D["monitor"] = value ?? ""; }
        }

        // Perfil CS2 (defaults los valores que pediste)
        public static int Cs2W   { get { return GetI("cs2.w", 1440); }   set { SetI("cs2.w", value); } }
        public static int Cs2H   { get { return GetI("cs2.h", 1080); }   set { SetI("cs2.h", value); } }
        public static int Cs2Vib { get { return GetI("cs2.vib", 100); }  set { SetI("cs2.vib", value); } }

        // Perfil Normal
        public static int NorW   { get { return GetI("normal.w", 2560); }  set { SetI("normal.w", value); } }
        public static int NorH   { get { return GetI("normal.h", 1440); }  set { SetI("normal.h", value); } }
        public static int NorVib { get { return GetI("normal.vib", 50); }  set { SetI("normal.vib", value); } }
    }

    // -----------------------------------------------------------------------
    //  Iniciar con Windows: clave Run del registro (native, sin accesos directos).
    // -----------------------------------------------------------------------
    static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "NvidiaCS2Toggle";

        public static bool Enabled
        {
            get
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey))
                    return k != null && k.GetValue(Name) != null;
            }
        }

        public static void Set(bool on)
        {
            using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (k == null) return;
                if (on) k.SetValue(Name, "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue(Name, false);
            }
        }
    }

    // -----------------------------------------------------------------------
    //  Pantallas / resolucion (Win32)
    // -----------------------------------------------------------------------
    static class Display
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        const int ENUM_CURRENT_SETTINGS = -1;
        const int CDS_UPDATEREGISTRY    = 0x00000001;
        const int DISP_CHANGE_SUCCESSFUL = 0;
        const int DM_PELSWIDTH  = 0x80000;
        const int DM_PELSHEIGHT = 0x100000;

        const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        const int DISPLAY_DEVICE_PRIMARY_DEVICE      = 0x00000004;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode,
                                                  IntPtr hwnd, int dwflags, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum,
                                              ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        // ---- API CCD: nombres "amigables" como en Configuracion de Windows ----
        [StructLayout(LayoutKind.Sequential)]
        struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId; public uint id; public uint modeInfoIdx;
            public uint outputTechnology; public uint rotation; public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate; public uint scanLineOrdering;
            public int targetAvailable; public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        struct DISPLAYCONFIG_MODE_INFO { public uint infoType; }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_DEVICE_INFO_HEADER { public uint type; public uint size; public LUID adapterId; public uint id; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string viewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public uint outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
        const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
        const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

        [DllImport("user32.dll")]
        static extern int GetDisplayConfigBufferSizes(uint flags, out int numPathArrayElements, out int numModeInfoArrayElements);

        [DllImport("user32.dll")]
        static extern int QueryDisplayConfig(uint flags, ref int numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref int numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName);

        [DllImport("user32.dll")]
        static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);

        // Mapa: \\.\DISPLAYn -> nombre amigable del monitor (el de Configuracion de Windows)
        static Dictionary<string, string> GetFriendlyNames()
        {
            var map = new Dictionary<string, string>();
            try
            {
                int numPaths, numModes;
                if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out numPaths, out numModes) != 0)
                    return map;

                var paths = new DISPLAYCONFIG_PATH_INFO[numPaths];
                var modes = new DISPLAYCONFIG_MODE_INFO[numModes];
                if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref numPaths, paths, ref numModes, modes, IntPtr.Zero) != 0)
                    return map;

                for (int i = 0; i < numPaths; i++)
                {
                    var src = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                    src.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
                    src.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME));
                    src.header.adapterId = paths[i].sourceInfo.adapterId;
                    src.header.id = paths[i].sourceInfo.id;
                    if (DisplayConfigGetDeviceInfo(ref src) != 0) continue;
                    string gdi = src.viewGdiDeviceName;
                    if (string.IsNullOrEmpty(gdi)) continue;

                    var tgt = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
                    tgt.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
                    tgt.header.size = (uint)Marshal.SizeOf(typeof(DISPLAYCONFIG_TARGET_DEVICE_NAME));
                    tgt.header.adapterId = paths[i].targetInfo.adapterId;
                    tgt.header.id = paths[i].targetInfo.id;
                    if (DisplayConfigGetDeviceInfo(ref tgt) == 0 &&
                        !string.IsNullOrEmpty(tgt.monitorFriendlyDeviceName))
                        map[gdi] = tgt.monitorFriendlyDeviceName;
                }
            }
            catch { }
            return map;
        }

        public static List<MonitorInfo> EnumerateMonitors()
        {
            var list = new List<MonitorInfo>();
            var friendlyNames = GetFriendlyNames();
            uint i = 0;
            while (true)
            {
                var adapter = new DISPLAY_DEVICE();
                adapter.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                if (!EnumDisplayDevices(null, i, ref adapter, 0)) break;
                i++;

                if ((adapter.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0) continue;

                // Nombre real (Configuracion de Windows); si no, el del monitor PnP.
                string friendly;
                if (!friendlyNames.TryGetValue(adapter.DeviceName, out friendly) || string.IsNullOrEmpty(friendly))
                {
                    friendly = adapter.DeviceString;
                    var mon = new DISPLAY_DEVICE();
                    mon.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                    if (EnumDisplayDevices(adapter.DeviceName, 0, ref mon, 0) &&
                        !string.IsNullOrEmpty(mon.DeviceString))
                        friendly = mon.DeviceString;
                }

                list.Add(new MonitorInfo
                {
                    Device = adapter.DeviceName,
                    Friendly = friendly,
                    Primary = (adapter.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0
                });
            }
            return list;
        }

        // Resoluciones que el monitor/PC realmente soporta, como "ANCHOxALTO",
        // sin duplicados y de mayor a menor. (Las custom del panel NVIDIA tambien salen.)
        public static List<string> GetSupportedResolutions(string device)
        {
            var seen = new HashSet<string>();
            var list = new List<int[]>();
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            for (int i = 0; EnumDisplaySettings(device, i, ref dm); i++)
            {
                string key = dm.dmPelsWidth + "x" + dm.dmPelsHeight;
                if (seen.Add(key)) list.Add(new int[] { dm.dmPelsWidth, dm.dmPelsHeight });
            }
            list.Sort((a, b) => a[0] != b[0] ? b[0] - a[0] : b[1] - a[1]);
            var outl = new List<string>();
            foreach (var r in list) outl.Add(r[0] + "x" + r[1]);
            return outl;
        }

        public static bool GetCurrentResolution(string device, out int width, out int height)
        {
            width = 0; height = 0;
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (!EnumDisplaySettings(device, ENUM_CURRENT_SETTINGS, ref dm))
                return false;
            width = dm.dmPelsWidth;
            height = dm.dmPelsHeight;
            return true;
        }

        public static bool SetResolution(string device, int width, int height, out string error)
        {
            error = null;
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (!EnumDisplaySettings(device, ENUM_CURRENT_SETTINGS, ref dm))
            {
                error = "No se pudo leer la configuracion actual de la pantalla.";
                return false;
            }

            if (dm.dmPelsWidth == width && dm.dmPelsHeight == height)
                return true; // ya esta

            dm.dmPelsWidth  = width;
            dm.dmPelsHeight = height;
            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;

            int result = ChangeDisplaySettingsEx(device, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
            if (result != DISP_CHANGE_SUCCESSFUL)
            {
                error = string.Format("No se pudo cambiar a {0}x{1} (codigo {2}). " +
                                      "Verifica que esa resolucion este disponible / habilitada en el panel NVIDIA.",
                                      width, height, result);
                return false;
            }
            return true;
        }
    }

    // -----------------------------------------------------------------------
    //  Digital Vibrance (NVAPI)
    // -----------------------------------------------------------------------
    static class Nvidia
    {
        // El unico export real de NVAPI; todo lo demas se obtiene por ID.
        [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr NvAPI_QueryInterface(uint id);

        // IDs de funcion (magic numbers de NVAPI)
        const uint ID_Initialize              = 0x0150E828;
        const uint ID_Unload                  = 0xD22BDD7E;
        const uint ID_EnumNvidiaDisplayHandle = 0x9ABDD40D;
        const uint ID_GetAssociatedNvidiaDisplayName = 0x22A78B05;
        const uint ID_GetDVCInfo              = 0x4085DE45;
        const uint ID_SetDVCLevel             = 0x172409B4;

        [StructLayout(LayoutKind.Sequential)]
        struct NV_DISPLAY_DVC_INFO
        {
            public uint version;
            public int  currentLevel;
            public int  minLevel;
            public int  maxLevel;
        }

        static uint DVC_INFO_VER = (uint)(Marshal.SizeOf(typeof(NV_DISPLAY_DVC_INFO)) | (1 << 16));

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int Initialize_t();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int Unload_t();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int EnumDisplay_t(int thisEnum, ref IntPtr pNvDispHandle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        delegate int GetAssocName_t(IntPtr hNvDisplay, StringBuilder szDisplayName);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int GetDVCInfo_t(IntPtr hNvDisplay, uint outputId, ref NV_DISPLAY_DVC_INFO info);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SetDVCLevel_t(IntPtr hNvDisplay, uint outputId, int level);

        static T Get<T>(uint id) where T : class
        {
            IntPtr p = NvAPI_QueryInterface(id);
            if (p == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer(p, typeof(T)) as T;
        }

        // Aplica el vibrance SOLO al monitor cuyo nombre de dispositivo (\\.\DISPLAYn)
        // coincide con 'deviceName'. Si deviceName es nulo/vacio, lo aplica a todos.
        public static bool SetVibrancePercent(string deviceName, int percent, out string error)
        {
            error = null;
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            Initialize_t init;
            EnumDisplay_t enumDisp;
            GetAssocName_t getName;
            GetDVCInfo_t getInfo;
            SetDVCLevel_t setLevel;
            Unload_t unload;

            try
            {
                init     = Get<Initialize_t>(ID_Initialize);
                enumDisp = Get<EnumDisplay_t>(ID_EnumNvidiaDisplayHandle);
                getName  = Get<GetAssocName_t>(ID_GetAssociatedNvidiaDisplayName);
                getInfo  = Get<GetDVCInfo_t>(ID_GetDVCInfo);
                setLevel = Get<SetDVCLevel_t>(ID_SetDVCLevel);
                unload   = Get<Unload_t>(ID_Unload);
            }
            catch (DllNotFoundException)
            {
                error = "No se encontro nvapi64.dll. Necesitas drivers NVIDIA instalados (GPU NVIDIA).";
                return false;
            }

            if (init == null || enumDisp == null || getInfo == null || setLevel == null)
            {
                error = "No se pudieron resolver las funciones de NVAPI.";
                return false;
            }

            if (init() != 0)
            {
                error = "NvAPI_Initialize fallo.";
                return false;
            }

            bool filter = !string.IsNullOrEmpty(deviceName);

            try
            {
                bool anyOk = false;
                bool matched = false;
                for (int i = 0; i < 16; i++)
                {
                    IntPtr h = IntPtr.Zero;
                    if (enumDisp(i, ref h) != 0 || h == IntPtr.Zero)
                        break; // fin de la enumeracion

                    if (filter && getName != null)
                    {
                        var sb = new StringBuilder(64);
                        if (getName(h, sb) != 0) continue;
                        if (!string.Equals(sb.ToString().Trim(), deviceName.Trim(),
                                           StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    matched = true;

                    NV_DISPLAY_DVC_INFO info = new NV_DISPLAY_DVC_INFO();
                    info.version = DVC_INFO_VER;
                    if (getInfo(h, 0, ref info) != 0)
                        continue;

                    // El Panel de NVIDIA usa 50% como neutro:
                    //   50%  -> nivel 0    (neutro / default)
                    //   100% -> maxLevel   (vibrance maximo)
                    //   <50% -> minLevel   (desaturado, si la GPU lo soporta)
                    int level;
                    if (percent >= 50)
                        level = (int)Math.Round((percent - 50) / 50.0 * info.maxLevel);
                    else
                        level = (int)Math.Round((50 - percent) / 50.0 * info.minLevel);
                    if (level < info.minLevel) level = info.minLevel;
                    if (level > info.maxLevel) level = info.maxLevel;

                    if (setLevel(h, 0, level) == 0)
                        anyOk = true;
                }

                if (!anyOk)
                {
                    error = (filter && !matched)
                        ? "El monitor seleccionado no se encontro en NVAPI (¿conectado a la GPU NVIDIA?)."
                        : "No se pudo aplicar el Digital Vibrance.";
                    return false;
                }
                return true;
            }
            finally
            {
                if (unload != null) unload();
            }
        }
    }

    // Resuelve el dispositivo del "monitor de juego" (guardado, o principal, o el primero).
    static class Game
    {
        public static string Device(List<MonitorInfo> monitors)
        {
            string saved = Settings.GameMonitor;
            if (!string.IsNullOrEmpty(saved))
                foreach (var m in monitors)
                    if (m.Device == saved) return saved;
            foreach (var m in monitors)
                if (m.Primary) return m.Device;
            return monitors.Count > 0 ? monitors[0].Device : null;
        }
    }

    // -----------------------------------------------------------------------
    //  App de bandeja (icono + popup propio)
    // -----------------------------------------------------------------------
    class TrayApp : ApplicationContext
    {
        readonly NotifyIcon _icon;
        MenuForm _menu;

        public TrayApp()
        {
            _icon = new NotifyIcon { Icon = MakeIcon(), Text = "CS2 Res Tweaker", Visible = true };
            _icon.MouseClick += (s, e) => ShowMenu(); // izquierdo o derecho: popup propio
        }

        void ShowMenu()
        {
            if (_menu != null && !_menu.IsDisposed) _menu.Close();
            _menu = new MenuForm(this);
            _menu.ShowAtCursor();
        }

        // ---- API que usa el popup ----
        public string ActiveMode()
        {
            string dev = Game.Device(Display.EnumerateMonitors());
            int w, h;
            if (dev != null && Display.GetCurrentResolution(dev, out w, out h))
            {
                if (w == Settings.Cs2W && h == Settings.Cs2H) return "CS2";
                if (w == Settings.NorW && h == Settings.NorH) return "Normal";
            }
            return "";
        }

        public bool StartupEnabled() { try { return Startup.Enabled; } catch { return false; } }

        public void ApplyCs2()    { Apply("CS2", Settings.Cs2W, Settings.Cs2H, Settings.Cs2Vib); }
        public void ApplyNormal() { Apply("Normal", Settings.NorW, Settings.NorH, Settings.NorVib); }

        public void OpenSettings() { using (var f = new SettingsForm()) f.ShowDialog(); }

        public void ToggleStartup()
        {
            try { Startup.Set(!Startup.Enabled); }
            catch (Exception ex) { Show(ToolTipIcon.Warning, "Iniciar con Windows", ex.Message); }
        }

        public void ExitApp() { Exit(); }

        void Apply(string name, int w, int h, int vibrance)
        {
            var monitors = Display.EnumerateMonitors();
            string dev = Game.Device(monitors);

            // Todo en una sola accion: primero la resolucion, luego el vibrance
            // (asi el cambio de modo no resetea el vibrance recien aplicado).
            string resErr, vibErr;
            bool resOk = Display.SetResolution(dev, w, h, out resErr);
            bool vibOk = Nvidia.SetVibrancePercent(dev, vibrance, out vibErr);

            string monName = "monitor de juego";
            foreach (var m in monitors)
                if (m.Device == dev) { monName = m.Friendly; break; }

            if (resOk && vibOk)
            {
                Show(ToolTipIcon.Info, "Modo " + name,
                     string.Format("{0}: {1}x{2}  |  Vibrance {3}%", monName, w, h, vibrance));
            }
            else
            {
                string msg = "";
                if (!resOk) msg += "Resolucion: " + resErr + "\n";
                if (!vibOk) msg += "Vibrance: " + vibErr;
                Show(ToolTipIcon.Warning, "Modo " + name + " (parcial)", msg.Trim());
            }
        }

        void Show(ToolTipIcon kind, string title, string text)
        {
            _icon.BalloonTipIcon = kind;
            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = text;
            _icon.ShowBalloonTip(3000);
        }

        // Usa el icono incrustado en el .exe (icon.ico via /win32icon). Sin archivo suelto.
        public static Icon MakeIcon()
        {
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { return SystemIcons.Application; }
        }

        void Exit()
        {
            _icon.Visible = false;
            _icon.Dispose();
            Application.Exit();
        }
    }

    // -----------------------------------------------------------------------
    //  Popup propio de la app (reemplaza el menu nativo de Windows)
    // -----------------------------------------------------------------------
    class MenuForm : Form
    {
        readonly TrayApp _app;

        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);

        public MenuForm(TrayApp app)
        {
            _app = app;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Bg;
            TopMost = true;
            Font = new Font("Segoe UI", 9.5f);

            string active = app.ActiveMode();
            bool startup = app.StartupEnabled();

            int w = 210, y = 6;
            y = AddRow("Modo CS2",    active == "CS2",    () => { Close(); _app.ApplyCs2(); }, y, w);
            y = AddRow("Modo Normal", active == "Normal", () => { Close(); _app.ApplyNormal(); }, y, w);
            y = AddSep(y, w);
            y = AddRow("Configuracion...", false, () => { Close(); _app.OpenSettings(); }, y, w);
            y = AddRow("Iniciar con Windows", startup, () => { _app.ToggleStartup(); Close(); }, y, w);
            y = AddSep(y, w);
            y = AddRow("Salir", false, () => { Close(); _app.ExitApp(); }, y, w);

            ClientSize = new Size(w, y + 8);
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); UI.RoundWindow(Handle); }

        int AddRow(string text, bool active, Action onClick, int y, int w)
        {
            var b = new PillButton
            {
                Text = (active ? "✓   " : "       ") + text, // tick naranja si esta activo
                Left = 8, Top = y, Width = w - 16, Height = 34,
                Normal = Theme.Bg, HoverColor = Theme.Hover,
                Fore = active ? Theme.Accent : Theme.Text,
                LeftAlign = true, Radius = 9, Font = Font
            };
            b.Click += (s, e) => onClick();
            Controls.Add(b);
            return y + 36;
        }

        int AddSep(int y, int w)
        {
            Controls.Add(new Panel { Left = 16, Top = y + 4, Width = w - 32, Height = 1, BackColor = Theme.Border });
            return y + 10;
        }

        public void ShowAtCursor()
        {
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            var p = Cursor.Position;
            int x = p.X - Width, yy = p.Y - Height; // ancla la esquina al cursor (bandeja, abajo-derecha)
            if (x < wa.Left) x = wa.Left;
            if (x + Width > wa.Right) x = wa.Right - Width;
            if (yy < wa.Top) yy = wa.Top;
            if (yy + Height > wa.Bottom) yy = wa.Bottom - Height;
            Location = new Point(x, yy);
            Show();
            SetForegroundWindow(Handle); // sin esto, el cierre al clic-afuera (Deactivate) no dispara
            Activate();
        }

        protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Close(); }
    }

    // -----------------------------------------------------------------------
    //  Tema oscuro
    // -----------------------------------------------------------------------
    static class Theme
    {
        public static readonly Color Bg      = Color.FromArgb(11, 22, 34);    // azul casi negro
        public static readonly Color Panel   = Color.FromArgb(20, 36, 58);
        public static readonly Color Text    = Color.FromArgb(224, 232, 242);
        public static readonly Color Accent  = Color.FromArgb(248, 157, 28);  // f89d1c
        public static readonly Color AccentH = Color.FromArgb(255, 181, 71);
        public static readonly Color Border  = Color.FromArgb(38, 58, 86);
        public static readonly Color Hover   = Color.FromArgb(28, 50, 80);
        public static readonly Color Track   = Color.FromArgb(18, 32, 52);
    }

    // Helpers de esquinas redondeadas (look moderno).
    static class UI
    {
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // Esquinas redondeadas nativas de Windows 11 (suaves, sin recorte dentado) + borde de acento.
        public static void RoundWindow(IntPtr hwnd)
        {
            int round = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, 33, ref round, 4); // DWMWA_WINDOW_CORNER_PREFERENCE
            int border = Theme.Accent.R | (Theme.Accent.G << 8) | (Theme.Accent.B << 16); // COLORREF
            DwmSetWindowAttribute(hwnd, 34, ref border, 4); // DWMWA_BORDER_COLOR
        }
    }

    // Boton dibujado a mano con esquinas redondeadas suaves (anti-aliasing).
    // Reemplaza el recorte por Region, que se ve dentado / estilo XP.
    class PillButton : Button
    {
        public Color Normal = Theme.Bg, HoverColor = Theme.Hover, Fore = Theme.Text;
        public int Radius = 9;
        public bool LeftAlign = false;
        bool _hover;

        public PillButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = false;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg); // las esquinas se funden con el fondo
            using (var path = UI.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (var b = new SolidBrush(_hover ? HoverColor : Normal))
                g.FillPath(b, path);
            int pad = LeftAlign ? 12 : 0;
            var rect = new Rectangle(pad, 0, Width - pad, Height);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                        (LeftAlign ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Text, Font, rect, Fore, flags);
        }
    }

    // Slider 0-100 dibujado a mano (el TrackBar nativo no se puede tematizar).
    class Slider : Control
    {
        public int Minimum = 0, Maximum = 100;
        int _value;
        public event EventHandler ValueChanged;

        public int Value
        {
            get { return _value; }
            set
            {
                int v = value < Minimum ? Minimum : (value > Maximum ? Maximum : value);
                if (v == _value) return;
                _value = v; Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        public Slider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 24;
        }

        void SetFromX(int x)
        {
            float t = (float)(x - 8) / Math.Max(1, Width - 16);
            Value = Minimum + (int)Math.Round(t * (Maximum - Minimum));
        }
        protected override void OnMouseDown(MouseEventArgs e) { Focus(); SetFromX(e.X); Capture = true; }
        protected override void OnMouseMove(MouseEventArgs e) { if (e.Button == MouseButtons.Left) SetFromX(e.X); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int cy = Height / 2, x0 = 8, x1 = Width - 8;
            using (var p = new Pen(Theme.Track, 4f)) g.DrawLine(p, x0, cy, x1, cy);
            float t = (float)(_value - Minimum) / Math.Max(1, Maximum - Minimum);
            int kx = x0 + (int)(t * (x1 - x0));
            using (var p = new Pen(Theme.Accent, 4f)) g.DrawLine(p, x0, cy, kx, cy);
            using (var b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, kx - 7, cy - 7, 14, 14);
        }
    }

    // -----------------------------------------------------------------------
    //  Ventana de configuracion (tema oscuro, sin chrome nativo)
    // -----------------------------------------------------------------------
    class SettingsForm : Form
    {
        ComboBox _mon, _cRes, _nRes;
        Slider _cVib, _nVib;
        readonly List<MonitorInfo> _monitors;

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        const int WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 0x2;

        public SettingsForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(320, 362);
            Padding = new Padding(1); // espacio para el borde de acento
            Text = "CS2 Res Tweaker";       // etiqueta en la barra de tareas
            Icon = TrayApp.MakeIcon();       // icono de la app en la barra de tareas
            ShowInTaskbar = true;

            BuildTitleBar();

            _monitors = Display.EnumerateMonitors();

            AddHeader("Monitor de juego", 50);
            _mon = MakeCombo(16, 76, 288);
            for (int i = 0; i < _monitors.Count; i++)
                _mon.Items.Add((i + 1) + ". " + _monitors[i].Friendly + (_monitors[i].Primary ? "  (principal)" : ""));
            _mon.SelectedIndexChanged += (s, e) => OnMonitorChanged();

            AddHeader("Modo CS2", 116);
            AddLabel("Resolucion", 16, 144);
            _cRes = MakeCombo(110, 140, 150);
            AddLabel("Vibrance", 16, 176);
            _cVib = AddVib(90, 170, Settings.Cs2Vib);

            AddHeader("Modo Normal", 218);
            AddLabel("Resolucion", 16, 246);
            _nRes = MakeCombo(110, 242, 150);
            AddLabel("Vibrance", 16, 278);
            _nVib = AddVib(90, 272, Settings.NorVib);

            var ok = MakeButton("Guardar", DialogResult.OK, true);
            ok.Left = 138; ok.Top = 320; ok.Click += (s, e) => SaveAll();
            var cancel = MakeButton("Cancelar", DialogResult.Cancel, false);
            cancel.Left = 226; cancel.Top = 320;
            Controls.Add(ok); Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;

            // Selecciona el monitor de juego actual; eso dispara la carga de resoluciones.
            string dev = Game.Device(_monitors);
            int idx = 0;
            for (int i = 0; i < _monitors.Count; i++) if (_monitors[i].Device == dev) idx = i;
            if (_monitors.Count > 0) _mon.SelectedIndex = idx; else OnMonitorChanged();
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); UI.RoundWindow(Handle); }

        void OnMonitorChanged()
        {
            int i = _mon.SelectedIndex;
            string dev = (i >= 0 && i < _monitors.Count) ? _monitors[i].Device : null;
            if (dev != null) { Settings.GameMonitor = dev; Settings.Save(); }
            var res = Display.GetSupportedResolutions(dev);
            FillRes(_cRes, res, Settings.Cs2W, Settings.Cs2H);
            FillRes(_nRes, res, Settings.NorW, Settings.NorH);
        }

        void FillRes(ComboBox c, List<string> items, int w, int h)
        {
            c.Items.Clear();
            c.Items.AddRange(items.ToArray());
            string cur = w + "x" + h;
            if (!c.Items.Contains(cur)) c.Items.Insert(0, cur); // conserva el valor guardado aunque no se liste
            c.SelectedItem = cur;
            if (c.SelectedIndex < 0 && c.Items.Count > 0) c.SelectedIndex = 0;
        }

        void BuildTitleBar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.Panel };
            var title = new Label { Text = "CS2 Res Tweaker", AutoSize = true, ForeColor = Theme.Text,
                                    Left = 12, Top = 9, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            var close = new Label { Text = "X", AutoSize = false, Width = 36, Height = 36, Dock = DockStyle.Right,
                                    TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Text, Cursor = Cursors.Hand };
            close.MouseEnter += (s, e) => { close.BackColor = Color.FromArgb(200, 50, 50); close.ForeColor = Color.White; };
            close.MouseLeave += (s, e) => { close.BackColor = Theme.Panel; close.ForeColor = Theme.Text; };
            close.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            EventHandler<MouseEventArgs> drag = (s, e) =>
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero); }
            };
            bar.MouseDown += (s, e) => drag(s, e);
            title.MouseDown += (s, e) => drag(s, e);

            bar.Controls.Add(title); bar.Controls.Add(close);
            Controls.Add(bar);
        }

        void AddHeader(string text, int y)
        {
            Controls.Add(new Label { Text = text, Left = 14, Top = y, AutoSize = true,
                ForeColor = Theme.Accent, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
        }

        void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, Left = x, Top = y, AutoSize = true, ForeColor = Theme.Text });
        }

        ComboBox MakeCombo(int x, int y, int width)
        {
            var c = new ComboBox { Left = x, Top = y, Width = width, DropDownStyle = ComboBoxStyle.DropDownList,
                                   FlatStyle = FlatStyle.Flat, BackColor = Theme.Panel, ForeColor = Theme.Text,
                                   DrawMode = DrawMode.OwnerDrawFixed };
            c.DrawItem += (s, e) =>
            {
                bool sel = (e.State & DrawItemState.Selected) != 0;
                using (var bg = new SolidBrush(sel ? Theme.Accent : Theme.Panel)) e.Graphics.FillRectangle(bg, e.Bounds);
                if (e.Index >= 0)
                    using (var tb = new SolidBrush(sel ? Color.Black : Theme.Text))
                        e.Graphics.DrawString(c.Items[e.Index].ToString(), c.Font, tb, e.Bounds.Left + 2, e.Bounds.Top + 1);
            };
            Controls.Add(c);
            return c;
        }

        Slider AddVib(int x, int y, int val)
        {
            var t = new Slider { Left = x, Top = y, Width = 170, Value = val };
            var num = new Label { Left = x + 178, Top = y + 4, AutoSize = true, ForeColor = Theme.Accent,
                                  Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Text = val.ToString() };
            t.ValueChanged += (s, e) => num.Text = t.Value.ToString();
            Controls.Add(t); Controls.Add(num);
            return t;
        }

        PillButton MakeButton(string text, DialogResult dr, bool accent)
        {
            return new PillButton
            {
                Text = text, DialogResult = dr, Width = 84, Height = 30, Radius = 8,
                Normal = accent ? Theme.Accent : Theme.Panel,
                HoverColor = accent ? Theme.AccentH : Theme.Hover,
                Fore = accent ? Color.Black : Theme.Text
            };
        }

        static void ParseRes(ComboBox c, out int w, out int h)
        {
            var p = ((string)c.SelectedItem).Split('x');
            w = int.Parse(p[0]); h = int.Parse(p[1]);
        }

        void SaveAll()
        {
            int w, h;
            ParseRes(_cRes, out w, out h); Settings.Cs2W = w; Settings.Cs2H = h; Settings.Cs2Vib = _cVib.Value;
            ParseRes(_nRes, out w, out h); Settings.NorW = w; Settings.NorH = h; Settings.NorVib = _nVib.Value;
            Settings.Save();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}
