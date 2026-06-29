# CS2 Res Tweaker

[Español](#español) · [English](#english)

Hecho para **Counter-Strike 2**: con un clic activas el setup competitivo (resolución estirada + Digital Vibrance alto de NVIDIA) y vuelves a tu escritorio normal cuando terminas. App ligera de bandeja para Windows que aplica los cambios solo al monitor que elijas.

Built for **Counter-Strike 2**: one click flips on the competitive setup (stretched resolution + high NVIDIA Digital Vibrance) and back to your normal desktop when you're done. A lightweight Windows tray app that changes only the monitor you pick.

> Probado en / Tested on: **NVIDIA RTX 3090, Windows 11**.

---

## Español

### Descargar y usar
1. Descarga `CS2ResTweaker.exe` desde la pestaña **[Releases](../../releases)**.
2. Doble clic. Aparece el icono de la app en la bandeja del sistema (zona junto al reloj; puede estar en la flecha **⌃** de iconos ocultos).
3. **Clic** en el icono y se abre un menú propio:
   - **Modo CS2** / **Modo Normal**: aplica la resolución + Digital Vibrance de ese perfil. El activo lleva un ✓.
   - **Configuración…**: elige el monitor de juego y edita la resolución y el Digital Vibrance de cada perfil.
   - **Iniciar con Windows**: arranca solo al iniciar sesión.
   - **Salir**.

Es **portable**: un único `.exe`, sin instalador y sin dependencias (usa el .NET Framework incluido en Windows 10/11). No necesita permisos de administrador. Tu configuración se guarda en `config.txt`, junto al `.exe`.

### Configuración
Abre **Configuración…** desde el menú:
- **Monitor de juego**: a qué monitor se le aplican los cambios.
- **Resolución**: lista desplegable con las resoluciones que tu monitor soporta (se detectan solas). Las resoluciones personalizadas que crees en el Panel de NVIDIA también aparecen aquí.
- **Digital Vibrance**: barra de 0 a 100. En la escala de NVIDIA, **50 = neutro** y **100 = máximo**.
- **Idioma**: Español o English. La app se reabre en el idioma elegido.

Valores por defecto: CS2 a 1440×1080 al 100% · Normal a 2560×1440 al 50%.

### Requisitos
- Windows 10 u 11. *(Las esquinas redondeadas son de Windows 11; en Windows 10 funciona igual pero con ventanas rectas.)*
- Para el **Digital Vibrance**: tarjeta gráfica **NVIDIA** con drivers instalados, y el monitor de juego conectado a esa NVIDIA.
- El **cambio de resolución** funciona en cualquier equipo.

> **Solo NVIDIA por ahora.** El cambio de resolución sirve en cualquier GPU, pero el Digital Vibrance usa la librería de NVIDIA. Soporte para AMD (Radeon) o Intel podría añadirse más adelante.

### Mensajes de error (qué significan)
Aparecen como una notificación de Windows:
- **"No se detectó una tarjeta gráfica NVIDIA…"**: no tienes NVIDIA o faltan drivers. La resolución igual cambia; solo el Digital Vibrance no.
- **"El monitor elegido no está conectado a la tarjeta NVIDIA…"**: ese monitor cuelga de la gráfica integrada. Conéctalo a la NVIDIA o elige otro en Configuración.
- **"Tu monitor no aceptó la resolución…"**: esa resolución no está disponible. Créala en *Panel NVIDIA → Cambiar resolución → Personalizar* (para CS2, pon el escalado en "Pantalla completa").
- **"Modo … se aplicó solo en parte"**: una parte funcionó y la otra no; el detalle indica cuál.

### Compilar desde el código
No necesitas Visual Studio ni el SDK de .NET, Windows ya trae el compilador:
```powershell
.\build.ps1
```
Genera `CS2ResTweaker.exe` (el icono se toma de `icon.ico`). El código fuente está en UTF-8 con BOM para que los acentos compilen bien.

---

## English

### Download & use
1. Download `CS2ResTweaker.exe` from the **[Releases](../../releases)** tab.
2. Double-click it. The app icon appears in the system tray (near the clock; it may be under the hidden-icons **⌃** arrow).
3. **Click** the icon and a custom menu opens:
   - **Modo CS2** / **Modo Normal**: applies that profile's resolution + Digital Vibrance. The active one shows a ✓.
   - **Configuración…** (Settings): pick the gaming monitor and edit each profile's resolution and Digital Vibrance.
   - **Iniciar con Windows**: start automatically at login.
   - **Salir**: quit.

It's **portable**: a single `.exe`, no installer, no dependencies (it uses the .NET Framework built into Windows 10/11). No admin rights needed. Your settings are stored in `config.txt`, next to the `.exe`.

> The UI defaults to Spanish. Switch it to **English** in *Configuración → Idioma (Language)* and the app reopens translated.

### Settings
Open **Configuración…** (Settings) from the menu:
- **Gaming monitor**: which monitor the changes apply to.
- **Resolution**: a dropdown of the resolutions your monitor actually supports (auto-detected). Custom resolutions you create in the NVIDIA Control Panel show up here too.
- **Digital Vibrance**: a 0 to 100 slider. On NVIDIA's scale, **50 = neutral** and **100 = maximum**.
- **Language**: Spanish or English. The app reopens in the chosen language.

Defaults: CS2 at 1440×1080 / 100% · Normal at 2560×1440 / 50%.

### Requirements
- Windows 10 or 11. *(Rounded corners are a Windows 11 feature; on Windows 10 it works the same but with square windows.)*
- For **Digital Vibrance**: an **NVIDIA** GPU with drivers installed, and the gaming monitor connected to that NVIDIA card.
- **Resolution switching** works on any PC.

> **NVIDIA only for now.** Resolution switching works on any GPU, but Digital Vibrance uses NVIDIA's library. AMD (Radeon) / Intel support may be added later.

### Error messages (what they mean)
Shown as a Windows notification:
- **"No NVIDIA graphics card detected…"**: no NVIDIA or missing drivers. Resolution still changes; only Digital Vibrance doesn't.
- **"The chosen monitor isn't connected to the NVIDIA card…"**: that monitor runs off the integrated GPU. Connect it to the NVIDIA card or pick another in Settings.
- **"Your monitor didn't accept the resolution…"**: that resolution isn't available. Create it in *NVIDIA Control Panel → Change resolution → Customize* (for CS2, set scaling to "Full-screen").
- **"Mode … was only partially applied"**: one part worked and the other didn't; the detail says which.

### Build from source
No Visual Studio or .NET SDK needed, Windows ships the compiler:
```powershell
.\build.ps1
```
Produces `CS2ResTweaker.exe` (icon taken from `icon.ico`). The source is UTF-8 with BOM so accented characters compile correctly.

---

## Licencia / License
MIT, see [LICENSE](LICENSE).
