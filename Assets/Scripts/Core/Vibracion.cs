using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Vibración del celular: al recibir daño, con explosiones grandes y al derrotar jefes.
// Android usa el vibrador del sistema (duración en milisegundos); en el navegador del celular
// se usa navigator.vibrate (Plugins/WebGL/Vibracion.jslib). En PC no hace nada.
// Se puede apagar en Opciones (AjustesJuego.Vibracion).
public static class Vibracion
{
    static float ultimaVez = -1f;

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrador;
    static bool iniciado;
    static int versionSdk;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void VibrarNavegador(int milisegundos);
#endif

    public static void Vibrar(int milisegundos)
    {
        if (!AjustesJuego.Vibracion || !ControlesJuego.EsMovil) return;
        // Evita que muchas explosiones seguidas se encimen en una vibración eterna
        if (Time.unscaledTime - ultimaVez < 0.06f) return;
        ultimaVez = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!iniciado)
            {
                iniciado = true;
                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                    versionSdk = version.GetStatic<int>("SDK_INT");
                using (AndroidJavaClass jugador = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject actividad = jugador.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrador = actividad.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (vibrador == null) { Handheld.Vibrate(); return; }
            if (versionSdk >= 26)
            {
                using (AndroidJavaClass efecto = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject unaVez = efecto.CallStatic<AndroidJavaObject>("createOneShot", (long)milisegundos, -1))
                    vibrador.Call("vibrate", unaVez);
            }
            else vibrador.Call("vibrate", (long)milisegundos);
        }
        catch (System.Exception)
        {
            Handheld.Vibrate(); // respaldo (también hace que Unity agregue el permiso VIBRATE al APK)
        }
#elif UNITY_WEBGL && !UNITY_EDITOR
        VibrarNavegador(milisegundos);
#endif
    }
}
