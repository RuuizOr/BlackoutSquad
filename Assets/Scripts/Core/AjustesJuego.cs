using System;
using UnityEngine;

// Ajustes del jugador que se guardan en el dispositivo (PlayerPrefs):
// tamaño y transparencia de los controles táctiles, y vibración.
// Los volúmenes los guarda AudioManager.
public static class AjustesJuego
{
    const string ClaveTamaño = "TactilTamano", ClaveOpacidad = "TactilOpacidad", ClaveVibracion = "Vibracion";

    public const float TamañoMin = 0.75f, TamañoMax = 1.3f;
    public const float OpacidadMin = 0.2f, OpacidadMax = 1f;

    // Se lanza cada vez que cambia un ajuste (los controles táctiles se redibujan)
    public static event Action Cambiaron;

    public static float TamañoControles
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(ClaveTamaño, 1f), TamañoMin, TamañoMax);
        set { PlayerPrefs.SetFloat(ClaveTamaño, Mathf.Clamp(value, TamañoMin, TamañoMax)); Cambiaron?.Invoke(); }
    }

    public static float OpacidadControles
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(ClaveOpacidad, 0.7f), OpacidadMin, OpacidadMax);
        set { PlayerPrefs.SetFloat(ClaveOpacidad, Mathf.Clamp(value, OpacidadMin, OpacidadMax)); Cambiaron?.Invoke(); }
    }

    public static bool Vibracion
    {
        get => PlayerPrefs.GetInt(ClaveVibracion, 1) == 1;
        set { PlayerPrefs.SetInt(ClaveVibracion, value ? 1 : 0); Cambiaron?.Invoke(); }
    }

    public static void Guardar() => PlayerPrefs.Save();
}
