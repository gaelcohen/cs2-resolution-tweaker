# CS2 Resolution Tweaker
ONLY AVAILABLE FOR NVIDIA GRAPHICS.

[Español](#español) · [English](#english)

Diseñado para **Counter-Strike 2**. Activa tu configuración competitiva (resolución estirada + Digital Vibrance alto de NVIDIA) y vuelve a tu escritorio normal con un solo clic. Es una aplicación ligera para la bandeja del sistema que solo modifica el monitor que elijas.

Built for **Counter-Strike 2**. Switch between your competitive setup (stretched resolution + high NVIDIA Digital Vibrance) and your normal desktop with a single click. It's a lightweight Windows tray app that only changes the monitor you choose.


---

# Español

## Descargar

1. Descarga `CS2ResTweaker.exe` desde **[Releases](../../releases)**.
2. Haz doble clic para abrirlo.
3. Encontrarás el icono en la bandeja del sistema (junto al reloj, o dentro del menú **⌃** de iconos ocultos).
4. Haz clic sobre el icono para abrir el menú:

* **Modo CS2** / **Modo Normal**: cambia la resolución y el Digital Vibrance según el perfil seleccionado. El perfil activo aparece con un ✓.
* **Configuración...**: selecciona el monitor de juego y personaliza la resolución y el Digital Vibrance de ambos perfiles.
* **Iniciar con Windows**.
* **Salir**.

La aplicación es completamente **portable**. Solo necesitas un único `.exe`: no requiere instalación, dependencias ni permisos de administrador. Toda la configuración se guarda en `config.txt`, junto al ejecutable.

## Configuración

Desde **Configuración...** puedes cambiar:

* **Monitor de juego**: el monitor al que se aplicarán los cambios.
* **Resolución**: muestra automáticamente todas las resoluciones compatibles con tu monitor, incluidas las resoluciones personalizadas creadas desde el Panel de Control de NVIDIA.
* **Digital Vibrance**: deslizador entre 0 y 100. En NVIDIA, **50** es el valor neutro y **100** el máximo.
* **Idioma**: Español o English. La aplicación se reinicia automáticamente al cambiarlo.

Configuración predeterminada:

* **CS2:** 1440×1080 al 100%
* **Normal:** 2560×1440 al 50%

## Requisitos

* Windows 10 u 11.
* Para controlar el **Digital Vibrance**, necesitas una GPU NVIDIA con sus drivers instalados y el monitor conectado directamente a esa GPU.
* El cambio de resolución funciona con cualquier tarjeta gráfica.

> Actualmente solo el Digital Vibrance es compatible con NVIDIA. El cambio de resolución funciona con cualquier GPU. El soporte para AMD e Intel podría añadirse más adelante.

## Mensajes de error

Las advertencias aparecen como notificaciones de Windows.

**No se detectó una tarjeta gráfica NVIDIA...**

No hay una GPU NVIDIA disponible o los drivers no están instalados. La resolución seguirá cambiando normalmente; únicamente fallará el Digital Vibrance.

**El monitor elegido no está conectado a la tarjeta NVIDIA...**

El monitor seleccionado está conectado a la GPU integrada. Conéctalo a la tarjeta NVIDIA o elige otro monitor desde Configuración.

**Tu monitor no aceptó la resolución...**

La resolución no está disponible. Puedes crearla desde **Panel de Control de NVIDIA → Cambiar resolución → Personalizar**. Para CS2 se recomienda usar el escalado en **Pantalla completa**.

**Modo ... se aplicó solo en parte**

Uno de los cambios se aplicó correctamente y el otro no. La notificación indica cuál fue el problema.

## Compilar desde el código

No necesitas Visual Studio ni instalar el .NET SDK. Windows ya incluye el compilador necesario.

```powershell
.\build.ps1
```

El script genera `CS2ResTweaker.exe` utilizando `icon.ico` como icono. El código fuente está guardado en UTF-8 con BOM para evitar problemas con caracteres acentuados.

---

# English

## Download

1. Download `CS2ResTweaker.exe` from **[Releases](../../releases)**.
2. Double-click it.
3. The app will appear in the Windows system tray (next to the clock, or under the **⌃** hidden icons menu).
4. Click the icon to open the menu.

* **CS2 Mode** / **Normal Mode**: switches to the selected profile's resolution and Digital Vibrance settings. The active profile is marked with ✓.
* **Settings...**: choose your gaming monitor and customize both profiles.
* **Start with Windows**.
* **Exit**.

The app is completely **portable**. It's just a single `.exe`, with no installer, no dependencies and no administrator privileges required. Your settings are stored in `config.txt` next to the executable.

> The interface starts in Spanish by default. You can switch to English from **Settings → Language**. The app will restart automatically.

## Settings

Inside **Settings...** you can configure:

* **Gaming monitor**.
* **Resolution**: automatically lists every resolution supported by your monitor, including custom resolutions created in the NVIDIA Control Panel.
* **Digital Vibrance**: slider from 0 to 100. On NVIDIA, **50** is neutral and **100** is the maximum.
* **Language**: Spanish or English.

Default values:

* **CS2:** 1440×1080 at 100%
* **Normal:** 2560×1440 at 50%

## Requirements

* Windows 10 or Windows 11.
* Digital Vibrance requires an NVIDIA GPU with drivers installed, and the gaming monitor connected directly to that GPU.
* Resolution switching works with any graphics card.

> At the moment, only Digital Vibrance is NVIDIA-only. Resolution switching works on any GPU. AMD and Intel support may be added in the future.

## Error messages

Errors are shown as standard Windows notifications.

**No NVIDIA graphics card detected...**

No NVIDIA GPU was found or the drivers are missing. Resolution switching will still work, but Digital Vibrance won't.

**The selected monitor isn't connected to the NVIDIA GPU...**

That monitor is connected to the integrated graphics instead of the NVIDIA card. Connect it to the NVIDIA GPU or select another monitor.

**Your monitor didn't accept the resolution...**

The selected resolution isn't available. Create it from **NVIDIA Control Panel → Change Resolution → Customize**. For CS2, Full-screen scaling is recommended.

**Mode ... was only partially applied**

One setting was applied successfully while the other failed. The notification explains which one.

## Build from source

No Visual Studio or .NET SDK is required. Windows already includes everything needed.

```powershell
.\build.ps1
```

The script builds `CS2ResTweaker.exe` and uses `icon.ico` as the application icon. The source files are encoded as UTF-8 with BOM to ensure accented characters compile correctly.

---

# License

MIT. See [LICENSE](LICENSE).
