using System;
using System.Collections.Generic;
using System.Drawing;
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

    // -----------------------------------------------------------------------
    //  App de bandeja
    // -----------------------------------------------------------------------
    class TrayApp : ApplicationContext
    {
        readonly NotifyIcon _icon;
        readonly ContextMenuStrip _menu;
        readonly ToolStripMenuItem _itemCs2;
        readonly ToolStripMenuItem _itemNormal;
        readonly ToolStripMenuItem _itemMonitors;
        readonly ToolStripMenuItem _itemStartup;

        public TrayApp()
        {
            _itemCs2 = new ToolStripMenuItem("Modo CS2", null,
                (s, e) => Apply("CS2", Settings.Cs2W, Settings.Cs2H, Settings.Cs2Vib));
            _itemNormal = new ToolStripMenuItem("Modo Normal", null,
                (s, e) => Apply("Normal", Settings.NorW, Settings.NorH, Settings.NorVib));
            _itemMonitors = new ToolStripMenuItem("Monitor de juego");
            _itemStartup = new ToolStripMenuItem("Iniciar con Windows", null, (s, e) => ToggleStartup());

            _menu = new ContextMenuStrip();
            _menu.Items.Add(_itemCs2);
            _menu.Items.Add(_itemNormal);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_itemMonitors);
            _menu.Items.Add("Configuracion...", null, (s, e) => OpenSettings());
            _menu.Items.Add(_itemStartup);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Salir", null, (s, e) => Exit());

            _menu.Opening += (s, e) => RebuildMenu();

            _icon = new NotifyIcon
            {
                Icon = MakeIcon(),
                Text = "NVIDIA CS2 Toggle",
                Visible = true,
                ContextMenuStrip = _menu
            };

            // Clic simple izquierdo abre el menu desplegable.
            _icon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) ShowMenu();
            };
        }

        // Usa el mismo metodo interno que el clic derecho (maneja el foco correctamente).
        void ShowMenu()
        {
            var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (mi != null) mi.Invoke(_icon, null);
            else _menu.Show(Cursor.Position);
        }

        // Devuelve el dispositivo del monitor de juego: el guardado si sigue presente,
        // si no el principal, si no el primero disponible.
        string GetGameDevice(List<MonitorInfo> monitors)
        {
            string saved = Settings.GameMonitor;
            if (!string.IsNullOrEmpty(saved))
                foreach (var m in monitors)
                    if (m.Device == saved) return saved;

            foreach (var m in monitors)
                if (m.Primary) return m.Device;

            return monitors.Count > 0 ? monitors[0].Device : null;
        }

        void RebuildMenu()
        {
            var monitors = Display.EnumerateMonitors();
            string gameDev = GetGameDevice(monitors);

            // Submenu de monitores
            _itemMonitors.DropDownItems.Clear();
            if (monitors.Count == 0)
            {
                _itemMonitors.DropDownItems.Add(new ToolStripMenuItem("(no se detectaron monitores)") { Enabled = false });
            }
            else
            {
                int n = 1;
                foreach (var m in monitors)
                {
                    string label = string.Format("{0}. {1}{2}", n++, m.Friendly, m.Primary ? "  (principal)" : "");
                    var it = new ToolStripMenuItem(label);
                    it.Checked = (m.Device == gameDev);
                    string dev = m.Device; // captura
                    it.Click += (s, e) => { Settings.GameMonitor = dev; Settings.Save(); };
                    _itemMonitors.DropDownItems.Add(it);
                }
            }

            // Ticks de los perfiles segun la resolucion actual del monitor de juego
            int w, h;
            bool cs2 = false, normal = false;
            if (gameDev != null && Display.GetCurrentResolution(gameDev, out w, out h))
            {
                cs2 = (w == Settings.Cs2W && h == Settings.Cs2H);
                normal = (w == Settings.NorW && h == Settings.NorH);
            }
            _itemCs2.Checked = cs2;
            _itemNormal.Checked = normal;

            try { _itemStartup.Checked = Startup.Enabled; } catch { }
        }

        void ToggleStartup()
        {
            try { Startup.Set(!Startup.Enabled); }
            catch (Exception ex) { Show(ToolTipIcon.Warning, "Iniciar con Windows", ex.Message); }
        }

        void OpenSettings()
        {
            using (var f = new SettingsForm()) f.ShowDialog();
        }

        void Apply(string name, int w, int h, int vibrance)
        {
            var monitors = Display.EnumerateMonitors();
            string dev = GetGameDevice(monitors);

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

        // Genera un icono simple "CS" sin necesitar un archivo .ico.
        static Icon MakeIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var b = new SolidBrush(Color.FromArgb(118, 185, 0))) // verde NVIDIA
                    g.FillEllipse(b, 0, 0, 31, 31);
                using (var f = new Font("Segoe UI", 11, FontStyle.Bold))
                using (var tb = new SolidBrush(Color.Black))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("CS", f, tb, new RectangleF(0, 0, 32, 32), sf);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        void Exit()
        {
            _icon.Visible = false;
            _icon.Dispose();
            Application.Exit();
        }
    }

    // -----------------------------------------------------------------------
    //  Ventana de configuracion (resolucion + vibrance de cada perfil).
    //  Monitor e "Iniciar con Windows" se eligen desde el menu de la bandeja.
    // -----------------------------------------------------------------------
    class SettingsForm : Form
    {
        NumericUpDown _cw, _ch, _cv, _nw, _nh, _nv;

        public SettingsForm()
        {
            Text = "Configuracion - NVIDIA CS2 Toggle";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(360, 175);

            AddLabel("Ancho", 120, 12); AddLabel("Alto", 195, 12); AddLabel("Vibrance %", 265, 12);

            AddLabel("Modo CS2:", 12, 44);
            _cw = AddNum(120, 40, 320, 16000, Settings.Cs2W);
            _ch = AddNum(195, 40, 240, 16000, Settings.Cs2H);
            _cv = AddNum(270, 40, 0, 100, Settings.Cs2Vib);

            AddLabel("Modo Normal:", 12, 80);
            _nw = AddNum(120, 76, 320, 16000, Settings.NorW);
            _nh = AddNum(195, 76, 240, 16000, Settings.NorH);
            _nv = AddNum(270, 76, 0, 100, Settings.NorVib);

            AddLabel("Vibrance: 50 = neutro, 100 = maximo.", 12, 112);

            var ok = new Button { Text = "Guardar", Left = 175, Top = 138, Width = 80, DialogResult = DialogResult.OK };
            ok.Click += (s, e) => SaveAll();
            var cancel = new Button { Text = "Cancelar", Left = 263, Top = 138, Width = 80, DialogResult = DialogResult.Cancel };
            Controls.Add(ok); Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;
        }

        void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, Left = x, Top = y, AutoSize = true });
        }

        NumericUpDown AddNum(int x, int y, int min, int max, int val)
        {
            if (val < min) val = min;
            if (val > max) val = max;
            var n = new NumericUpDown { Left = x, Top = y, Width = 65, Minimum = min, Maximum = max, Value = val };
            Controls.Add(n);
            return n;
        }

        void SaveAll()
        {
            Settings.Cs2W = (int)_cw.Value; Settings.Cs2H = (int)_ch.Value; Settings.Cs2Vib = (int)_cv.Value;
            Settings.NorW = (int)_nw.Value; Settings.NorH = (int)_nh.Value; Settings.NorVib = (int)_nv.Value;
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
