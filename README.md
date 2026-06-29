# CS2 Res Tweaker

App diminuta de bandeja del sistema (Windows) para alternar entre dos perfiles
cambiando **resolución** y **Digital Vibrance** de NVIDIA — pensada para activar
el modo estirado + vibrance alto de CS2 con un clic, y volver a tu escritorio normal.

Los cambios se aplican **solo al monitor que elijas** como "Monitor de juego".

## Descargar y usar (portable)

1. Descarga `CS2ResTweaker.exe` desde [Releases](../../releases) (o compílalo, ver abajo).
2. Doble clic. Aparece un icono verde **CS** en la bandeja (zona oculta de la barra de tareas).
3. **Clic** en el icono → menú:
   - **Modo CS2** / **Modo Normal** — aplica resolución + vibrance (el perfil activo lleva ✓).
   - **Monitor de juego ▸** — elige a qué monitor afectan los cambios.
   - **Configuración…** — edita resolución y vibrance de cada perfil.
   - **Iniciar con Windows** — arranca solo al iniciar sesión.
   - **Salir**.

Es portable: un único `.exe`, sin instalador y sin dependencias (usa el .NET
Framework que ya viene en Windows 10/11). La configuración se guarda en
`config.txt` junto al `.exe`.

## Requisitos

- Windows 10/11.
- GPU **NVIDIA** con drivers instalados (para el Digital Vibrance, vía `nvapi64.dll`).
  El cambio de resolución funciona en cualquier equipo.
- El monitor de juego debe estar conectado a la GPU NVIDIA para el vibrance.

## Notas

- Si la resolución que pones (p. ej. 1440x1080) no está disponible, Windows la
  rechaza: créala/actívala en *Panel NVIDIA → Cambiar resolución → Personalizar*
  y pon el escalado en "Pantalla completa" para el modo estirado.
- El % de vibrance usa la escala del Panel NVIDIA: **50 = neutro**, 100 = máximo.

## Compilar

No necesitas Visual Studio ni el SDK de .NET — Windows ya trae el compilador:

```powershell
.\build.ps1
```

Genera `CS2ResTweaker.exe`.

## Licencia

MIT — ver [LICENSE](LICENSE).
