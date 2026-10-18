using System;
using System.Collections.Generic;
using UnityEngine;

// Una animación: lista de sprites que se reproducen a cierta velocidad.
[Serializable]
public class ClipSprites
{
    public string nombre = "Idle";
    public Sprite[] frames;
    public float fps = 10f;
    public bool bucle = true;
}

// Animador muy sencillo basado en listas de sprites.
// Para cambiar la "skin" basta con arrastrar otros sprites a los clips en el Inspector.
public class AnimadorSprites : MonoBehaviour
{
    [SerializeField] SpriteRenderer render;
    [SerializeField] List<ClipSprites> clips = new List<ClipSprites>();
    [SerializeField] string clipInicial = "Idle";

    ClipSprites clipActual;
    float tiempo;
    int frameActual;

    public string ClipActual => clipActual != null ? clipActual.nombre : "";
    public bool Terminado { get; private set; }
    public SpriteRenderer Render => render;

    void Awake()
    {
        if (render == null) render = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        clipActual = null;
        Reproducir(clipInicial, true);
    }

    public void Reproducir(string nombre, bool reiniciar = false)
    {
        if (!reiniciar && clipActual != null && clipActual.nombre == nombre) return;
        ClipSprites clip = clips.Find(c => c.nombre == nombre);
        if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
        clipActual = clip;
        tiempo = 0f;
        frameActual = 0;
        Terminado = false;
        render.sprite = clip.frames[0];
    }

    public bool TieneClip(string nombre)
    {
        return clips.Exists(c => c.nombre == nombre);
    }

    void Update()
    {
        if (clipActual == null || clipActual.frames.Length <= 1) { Terminado = true; return; }
        tiempo += Time.deltaTime;
        float duracionFrame = 1f / Mathf.Max(1f, clipActual.fps);
        while (tiempo >= duracionFrame)
        {
            tiempo -= duracionFrame;
            frameActual++;
            if (frameActual >= clipActual.frames.Length)
            {
                if (clipActual.bucle) frameActual = 0;
                else { frameActual = clipActual.frames.Length - 1; Terminado = true; }
            }
        }
        render.sprite = clipActual.frames[frameActual];
    }
}
