using UnityEngine;

// Crea los sistemas globales (GameManager, AudioManager, Efectos, Transición) antes de cargar
// cualquier escena. Así cualquier escena se puede probar directamente con Play.
public static class Arranque
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CrearSistemas()
    {
        if (GameManager.Instancia != null) return;
        GameObject prefab = Resources.Load<GameObject>("SistemasJuego");
        if (prefab == null)
        {
            Debug.LogWarning("No se encontró Resources/SistemasJuego. Ejecuta 'Blackout Squad/Construir juego'.");
            return;
        }
        GameObject sistemas = Object.Instantiate(prefab);
        sistemas.name = "SistemasJuego";
        sistemas.AddComponent<GestorMando>();

        // Celular (Android o navegador del teléfono): controles en pantalla, 60 FPS y pantalla siempre encendida
        if (ControlesJuego.EsMovil)
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ControlesTactiles.Crear();
        }
    }
}
