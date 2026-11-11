using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Ambiente visual de un nivel (lo crea GestorVisual al cargar cada nivel; se destruye con la escena):
// 1. Luces dinámicas: fogonazos y explosiones iluminan el escenario un instante; las antorchas parpadean.
// 2. Post-procesado: bloom, viñeta y color propio de cada misión (selva cálida, base fría, fortaleza rojiza).
// 3. Clima: lluvia y hojas en la selva, polvo en la base militar, brasas y ceniza en la fortaleza.
public class AmbienteNivel : MonoBehaviour
{
    public static AmbienteNivel Instancia { get; private set; }

    enum Tema { Selva, Base, Fortaleza }
    Tema tema;

    public static void Crear(int numeroNivel)
    {
        GameObject go = new GameObject("AmbienteNivel");
        AmbienteNivel a = go.AddComponent<AmbienteNivel>();
        a.tema = numeroNivel == 2 ? Tema.Base : (numeroNivel >= 3 ? Tema.Fortaleza : Tema.Selva);
        a.Preparar();
    }

    void Awake() { Instancia = this; }

    void OnDestroy() { if (Instancia == this) Instancia = null; }

    void Preparar()
    {
        Camera cam = Camera.main;
        PrepararLuces();
        PrepararAntorchas();
        PrepararPostProceso(cam);
        if (cam != null) PrepararClima(cam);
    }

    void Update()
    {
        ActualizarDestellos();
        ActualizarAntorchas();
    }

    // =====================================================================
    // 1. Luces dinámicas
    // =====================================================================
    class Destello
    {
        public Light2D luz;
        public float intensidad, duracion, tiempo;
    }

    const int MaxDestellos = 10;
    readonly List<Destello> destellos = new List<Destello>();
    bool hayLuces;
    int[] capasDibujo;

    // Desde cualquier script: AmbienteNivel.Iluminar(posición, color, radio, intensidad, duración)
    public static void Iluminar(Vector3 pos, Color color, float radio, float intensidad, float duracion)
    {
        if (Instancia != null) Instancia.NuevoDestello(pos, color, radio, intensidad, duracion);
    }

    // Campo interno de Light2D con las capas de dibujo que ilumina (Unity solo lo llena al crear la luz en el editor)
    static readonly System.Reflection.FieldInfo campoCapas = typeof(Light2D).GetField("m_ApplyToSortingLayers",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    void PrepararLuces()
    {
        // Solo si el nivel ya usa iluminación 2D (tiene luz global)
        bool hayGlobal = false;
        foreach (Light2D l in FindObjectsByType<Light2D>())
            if (l.lightType == Light2D.LightType.Global) { hayGlobal = true; break; }
        if (!hayGlobal || campoCapas == null) return;

        SortingLayer[] capas = SortingLayer.layers;
        capasDibujo = new int[capas.Length];
        for (int i = 0; i < capas.Length; i++) capasDibujo[i] = capas[i].id;
        hayLuces = true;
    }

    Light2D NuevaLuz(string nombre)
    {
        if (!hayLuces) return null;
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        campoCapas.SetValue(l, capasDibujo.Clone());
        l.pointLightInnerRadius = 0f;
        l.pointLightOuterRadius = 3f;
        l.falloffIntensity = 0.5f;
        l.intensity = 0f;
        return l;
    }

    void NuevoDestello(Vector3 pos, Color color, float radio, float intensidad, float duracion)
    {
        if (!hayLuces) return;
        Destello d = null;
        foreach (Destello x in destellos) if (x.tiempo >= x.duracion) { d = x; break; }
        if (d == null)
        {
            if (destellos.Count < MaxDestellos)
            {
                d = new Destello { luz = NuevaLuz("Destello") };
                destellos.Add(d);
            }
            else
            {
                // Se reutiliza el destello al que le falta menos
                d = destellos[0];
                foreach (Destello x in destellos) if (x.duracion - x.tiempo < d.duracion - d.tiempo) d = x;
            }
        }
        d.luz.transform.position = new Vector3(pos.x, pos.y, 0f);
        d.luz.color = color;
        d.luz.pointLightOuterRadius = radio;
        d.luz.intensity = intensidad;
        d.luz.enabled = true;
        d.intensidad = intensidad;
        d.duracion = Mathf.Max(0.01f, duracion);
        d.tiempo = 0f;
    }

    void ActualizarDestellos()
    {
        foreach (Destello d in destellos)
        {
            if (d.tiempo >= d.duracion) { if (d.luz.enabled) d.luz.enabled = false; continue; }
            d.tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(d.tiempo / d.duracion);
            d.luz.intensity = d.intensidad * (1f - t) * (1f - t);
        }
    }

    // ---------- Antorchas ----------
    readonly List<Light2D> antorchas = new List<Light2D>();

    void PrepararAntorchas()
    {
        if (!hayLuces) return;
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>())
        {
            if (!sr.gameObject.name.StartsWith("antorcha") || antorchas.Count >= 12) continue;
            Light2D l = NuevaLuz("LuzAntorcha");
            Bounds b = sr.bounds;
            l.transform.position = new Vector3(b.center.x, b.max.y - b.size.y * 0.15f, 0f);
            l.color = new Color(1f, 0.6f, 0.25f);
            l.pointLightOuterRadius = 4.5f;
            l.intensity = 0.8f;
            antorchas.Add(l);
        }
    }

    void ActualizarAntorchas()
    {
        for (int i = 0; i < antorchas.Count; i++)
        {
            float ruido = Mathf.PerlinNoise(Time.time * 6f, i * 3.7f);
            antorchas[i].intensity = 0.6f + ruido * 0.5f;
            antorchas[i].pointLightOuterRadius = 4.2f + ruido * 0.8f;
        }
    }

    // =====================================================================
    // 2. Post-procesado
    // =====================================================================
    void PrepararPostProceso(Camera cam)
    {
        VolumeProfile base_ = Resources.Load<VolumeProfile>("PostProceso");
        if (base_ == null || cam == null) return;

        UniversalAdditionalCameraData datos = cam.GetUniversalAdditionalCameraData();
        if (datos != null) datos.renderPostProcessing = true;

        VolumeProfile perfil = Instantiate(base_);
        if (perfil.TryGet(out WhiteBalance balance) && perfil.TryGet(out ColorAdjustments ajustes))
        {
            switch (tema)
            {
                case Tema.Selva: // atardecer cálido y verde vivo
                    balance.temperature.Override(14f);
                    ajustes.saturation.Override(14f);
                    ajustes.contrast.Override(8f);
                    ajustes.colorFilter.Override(new Color(1f, 0.98f, 0.92f));
                    break;
                case Tema.Base: // metálico y frío
                    balance.temperature.Override(-16f);
                    ajustes.saturation.Override(-6f);
                    ajustes.contrast.Override(10f);
                    ajustes.colorFilter.Override(new Color(0.93f, 0.97f, 1f));
                    break;
                case Tema.Fortaleza: // rojizo y amenazante
                    balance.temperature.Override(10f);
                    ajustes.saturation.Override(4f);
                    ajustes.contrast.Override(14f);
                    ajustes.colorFilter.Override(new Color(1f, 0.86f, 0.82f));
                    break;
            }
        }

        GameObject go = new GameObject("PostProceso");
        go.transform.SetParent(transform, false);
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 10f;
        v.profile = perfil;
    }

    // =====================================================================
    // 3. Clima y partículas de ambiente
    // =====================================================================
    Material materialParticulas;

    void PrepararClima(Camera cam)
    {
        // Se usa el material de los sprites de la escena (siempre viene incluido en el build)
        SpriteRenderer cualquiera = FindAnyObjectByType<SpriteRenderer>();
        if (cualquiera == null || cualquiera.sharedMaterial == null) return;
        materialParticulas = cualquiera.sharedMaterial;

        float alto = cam.orthographic ? cam.orthographicSize * 2f : 10f;
        float ancho = alto * Mathf.Max(cam.aspect, 2.2f); // margen para pantallas muy anchas de celular

        switch (tema)
        {
            case Tema.Selva:
                Lluvia(cam, ancho, alto);
                Hojas(cam, ancho, alto);
                break;
            case Tema.Base:
                Polvo(cam, ancho, alto);
                break;
            case Tema.Fortaleza:
                Brasas(cam, ancho, alto);
                Ceniza(cam, ancho, alto);
                break;
        }
    }

    ParticleSystem NuevoSistema(string nombre, Camera cam, Vector3 posLocal, Vector3 caja, Texture2D textura, int maximo)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(posLocal.x, posLocal.y, -cam.transform.position.z);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximo;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.prewarm = true;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = caja;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        Material m = new Material(materialParticulas);
        m.mainTexture = textura;
        r.sharedMaterial = m;
        r.sortingOrder = 45; // delante del escenario y los personajes
        return ps;
    }

    static void Desvanecer(ParticleSystem ps, float alphaMax)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alphaMax, 0.15f), new GradientAlphaKey(alphaMax, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    void Lluvia(Camera cam, float ancho, float alto)
    {
        ParticleSystem ps = NuevoSistema("Lluvia", cam, new Vector3(0f, alto * 0.6f, 0f), new Vector3(ancho * 1.4f, 0.2f, 1f), TexturaPunto(), 260);
        var main = ps.main;
        main.startLifetime = alto / 16f + 0.3f;
        main.startSize = 0.05f;
        main.startColor = new Color(0.75f, 0.85f, 1f, 0.45f);
        var em = ps.emission; em.rateOverTime = 110f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-3.5f, -2.5f);
        vel.y = new ParticleSystem.MinMaxCurve(-18f, -15f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.035f;
        r.lengthScale = 1f;
        ps.Play();
    }

    void Hojas(Camera cam, float ancho, float alto)
    {
        ParticleSystem ps = NuevoSistema("Hojas", cam, new Vector3(0f, alto * 0.6f, 0f), new Vector3(ancho * 1.4f, 0.2f, 1f), TexturaHoja(), 40);
        var main = ps.main;
        main.startLifetime = 9f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.6f, 0.2f), new Color(0.7f, 0.65f, 0.2f));
        var em = ps.emission; em.rateOverTime = 2.5f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.4f, -0.8f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
        var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.6f; ruido.frequency = 0.4f;
        Desvanecer(ps, 1f);
        ps.Play();
    }

    void Polvo(Camera cam, float ancho, float alto)
    {
        ParticleSystem ps = NuevoSistema("Polvo", cam, Vector3.zero, new Vector3(ancho * 1.3f, alto * 1.1f, 1f), TexturaPunto(), 70);
        var main = ps.main;
        main.startLifetime = 7f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new Color(0.9f, 0.82f, 0.65f, 1f);
        var em = ps.emission; em.rateOverTime = 8f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.15f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.3f; ruido.frequency = 0.3f;
        Desvanecer(ps, 0.35f);
        ps.Play();
    }

    void Brasas(Camera cam, float ancho, float alto)
    {
        ParticleSystem ps = NuevoSistema("Brasas", cam, new Vector3(0f, -alto * 0.55f, 0f), new Vector3(ancho * 1.3f, 0.3f, 1f), TexturaPunto(), 80);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.3f));
        var em = ps.emission; em.rateOverTime = 12f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.6f);
        vel.y = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.5f; ruido.frequency = 0.8f;
        Desvanecer(ps, 1f);
        ps.Play();
    }

    void Ceniza(Camera cam, float ancho, float alto)
    {
        ParticleSystem ps = NuevoSistema("Ceniza", cam, new Vector3(0f, alto * 0.6f, 0f), new Vector3(ancho * 1.4f, 0.2f, 1f), TexturaPunto(), 60);
        var main = ps.main;
        main.startLifetime = 8f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = new Color(0.55f, 0.52f, 0.5f, 1f);
        var em = ps.emission; em.rateOverTime = 6f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.1f, -0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.4f; ruido.frequency = 0.5f;
        Desvanecer(ps, 0.6f);
        ps.Play();
    }

    // ---------- Texturas generadas por código ----------
    static Texture2D texPunto, texHoja;

    static Texture2D TexturaPunto()
    {
        if (texPunto != null) return texPunto;
        const int n = 16;
        texPunto = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * 1.4f);
        }
        texPunto.SetPixels(px);
        texPunto.Apply();
        return texPunto;
    }

    static Texture2D TexturaHoja()
    {
        if (texHoja != null) return texHoja;
        const int n = 16;
        texHoja = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        Color[] px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f - n / 2f) / (n * 0.48f), dy = (y + 0.5f - n / 2f) / (n * 0.22f);
            bool dentro = dx * dx + dy * dy <= 1f;
            bool vena = dentro && Mathf.Abs(y + 0.5f - n / 2f) < 0.6f;
            px[y * n + x] = dentro ? (vena ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white) : Color.clear;
        }
        texHoja.SetPixels(px);
        texHoja.Apply();
        return texHoja;
    }
}
