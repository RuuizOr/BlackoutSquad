using System;
using System.Collections;
using UnityEngine;

// Lista de efectos de sonido del juego. Cada uno se asigna a uno o más AudioClips en el Inspector.
public enum Sfx
{
    DisparoEnemigo, CañonJefe, Misil, SinMunicion, RecogerArma,
    Explosion, ExplosionGrande, ExplosionChica, LanzarGranada,
    Paso, Salto, Aterrizaje, Rodar,
    RecogerObjeto, DañoJugador, DañoEnemigo, EnemigoDerrotado, ImpactoBala, GolpeEscudo,
    RomperCofre, Checkpoint, VidaExtra, CaerFoso, AlarmaJefe, CambioFase,
    UiHover, UiClick, Pausa, Reanudar
}

[Serializable]
public class EntradaSonido
{
    public Sfx id;
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volumen = 1f;
    [Range(0f, 0.3f)] public float variacionTono = 0.08f;
}

// Singleton persistente con canales separados de música y efectos.
// Solo los volúmenes se guardan en PlayerPrefs.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instancia { get; private set; }

    const string ClaveMusica = "VolumenMusica";
    const string ClaveEfectos = "VolumenEfectos";

    [Header("Música")]
    public AudioClip musicaMenu;
    public AudioClip[] musicaNiveles = new AudioClip[3];
    public AudioClip musicaJefe;
    public AudioClip musicaFinal;
    public AudioClip jingleVictoria;
    public AudioClip jingleGameOver;
    public AudioClip jingleInicioNivel;

    [Header("Efectos de sonido")]
    [SerializeField] EntradaSonido[] sonidos;
    [SerializeField] int canalesEfectos = 12;

    [Header("Volúmenes por defecto")]
    [SerializeField, Range(0f, 1f)] float volumenMusicaInicial = 0.6f;
    [SerializeField, Range(0f, 1f)] float volumenEfectosInicial = 0.8f;

    AudioSource musicaA, musicaB, fuenteJingle, fuenteUI;
    AudioSource[] fuentesEfectos;
    int siguienteFuente;
    bool usandoA = true;
    Coroutine rutinaFundido;
    float atenuacionPausa = 1f;

    public float VolumenMusica { get; private set; }
    public float VolumenEfectos { get; private set; }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(this); return; }
        Instancia = this;

        musicaA = CrearFuente("MusicaA", true);
        musicaB = CrearFuente("MusicaB", true);
        fuenteJingle = CrearFuente("Jingle", false);
        fuenteUI = CrearFuente("UI", false);
        fuentesEfectos = new AudioSource[canalesEfectos];
        for (int i = 0; i < canalesEfectos; i++) fuentesEfectos[i] = CrearFuente("Efecto" + i, false);

        VolumenMusica = PlayerPrefs.GetFloat(ClaveMusica, volumenMusicaInicial);
        VolumenEfectos = PlayerPrefs.GetFloat(ClaveEfectos, volumenEfectosInicial);
        AplicarVolumenes();
    }

    AudioSource CrearFuente(string nombre, bool bucle)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = bucle;
        s.ignoreListenerPause = true; // la UI suena aunque el juego esté pausado
        return s;
    }

    // ---------- Volúmenes ----------
    public void CambiarVolumenMusica(float v)
    {
        VolumenMusica = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(ClaveMusica, VolumenMusica);
        AplicarVolumenes();
    }

    public void CambiarVolumenEfectos(float v)
    {
        VolumenEfectos = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(ClaveEfectos, VolumenEfectos);
        AplicarVolumenes();
    }

    public void GuardarVolumenes() { PlayerPrefs.Save(); }

    // Baja la música mientras el juego está en pausa
    public void AtenuarPorPausa(bool pausado)
    {
        atenuacionPausa = pausado ? 0.4f : 1f;
        AplicarVolumenes();
    }

    void AplicarVolumenes()
    {
        if (rutinaFundido == null)
        {
            Activa().volume = VolumenMusica * atenuacionPausa;
            Inactiva().volume = 0f;
        }
        fuenteJingle.volume = VolumenMusica;
    }

    AudioSource Activa() => usandoA ? musicaA : musicaB;
    AudioSource Inactiva() => usandoA ? musicaB : musicaA;

    // ---------- Música ----------
    public void ReproducirMusica(AudioClip clip, float fundido = 1f)
    {
        if (clip == null) return;
        if (Activa().clip == clip && Activa().isPlaying) return;
        fuenteJingle.Stop();
        if (rutinaFundido != null) StopCoroutine(rutinaFundido);
        rutinaFundido = StartCoroutine(Cruzar(clip, fundido));
    }

    IEnumerator Cruzar(AudioClip clip, float duracion)
    {
        AudioSource sale = Activa();
        AudioSource entra = Inactiva();
        usandoA = !usandoA;
        entra.clip = clip;
        entra.volume = 0f;
        entra.Play();
        float inicioSale = sale.volume;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float k = t / Mathf.Max(0.01f, duracion);
            entra.volume = Mathf.Lerp(0f, VolumenMusica * atenuacionPausa, k);
            sale.volume = Mathf.Lerp(inicioSale, 0f, k);
            yield return null;
        }
        sale.Stop();
        rutinaFundido = null;
        AplicarVolumenes();
    }

    public void DetenerMusica()
    {
        if (rutinaFundido != null) { StopCoroutine(rutinaFundido); rutinaFundido = null; }
        musicaA.Stop();
        musicaB.Stop();
    }

    // Jingle corto (victoria / game over): detiene la música
    public void ReproducirJingle(AudioClip clip)
    {
        if (clip == null) return;
        DetenerMusica();
        fuenteJingle.clip = clip;
        fuenteJingle.volume = VolumenMusica;
        fuenteJingle.Play();
    }

    // ---------- Efectos ----------
    public static void Sonar(Sfx id, float volumen = 1f)
    {
        if (Instancia != null) Instancia.SonarInterno(id, volumen, false);
    }

    // Sonidos de interfaz (suenan siempre, incluso en pausa)
    public static void SonarUI(Sfx id)
    {
        if (Instancia != null) Instancia.SonarInterno(id, 1f, true);
    }

    public static void SonarClip(AudioClip clip, float volumen = 1f, float variacionTono = 0.06f)
    {
        if (Instancia == null || clip == null) return;
        AudioSource s = Instancia.SiguienteFuente();
        s.pitch = 1f + UnityEngine.Random.Range(-variacionTono, variacionTono);
        s.PlayOneShot(clip, volumen * Instancia.VolumenEfectos);
    }

    void SonarInterno(Sfx id, float volumen, bool esUI)
    {
        EntradaSonido e = Buscar(id);
        if (e == null || e.clips == null || e.clips.Length == 0) return;
        AudioClip clip = e.clips[UnityEngine.Random.Range(0, e.clips.Length)];
        if (clip == null) return;
        AudioSource s = esUI ? fuenteUI : SiguienteFuente();
        s.pitch = 1f + UnityEngine.Random.Range(-e.variacionTono, e.variacionTono);
        s.PlayOneShot(clip, e.volumen * volumen * VolumenEfectos);
    }

    AudioSource SiguienteFuente()
    {
        AudioSource s = fuentesEfectos[siguienteFuente];
        siguienteFuente = (siguienteFuente + 1) % fuentesEfectos.Length;
        return s;
    }

    EntradaSonido Buscar(Sfx id)
    {
        if (sonidos == null) return null;
        for (int i = 0; i < sonidos.Length; i++)
            if (sonidos[i].id == id) return sonidos[i];
        return null;
    }
}
