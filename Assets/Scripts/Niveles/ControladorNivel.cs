using System.Collections;
using UnityEngine;

// Dirige un nivel: inicio (briefing y tarjeta de título), muerte y reaparición del jugador,
// checkpoints, pelea de jefe, bonificaciones, Game Over y Victoria.
public class ControladorNivel : MonoBehaviour
{
    public static ControladorNivel Instancia { get; private set; }

    [Header("Nivel")]
    [SerializeField] int numeroNivel = 1;
    [SerializeField] string titulo = "MISIÓN 1";
    [SerializeField] string subtitulo = "SELVA HOSTIL";
    [SerializeField] AudioClip musicaNivel;
    [SerializeField, TextArea(3, 8)] string textoBriefing = "";
    [SerializeField, TextArea(3, 8)] string textoFinal = "";

    [Header("Dificultad")]
    [SerializeField] float multiplicadorVida = 1f;
    [SerializeField] float multiplicadorAgresividad = 1f;
    [SerializeField] float multiplicadorBotiquines = 1f;

    [Header("Puntuación")]
    [SerializeField] int bonoNivelSinMorir = 5000;
    [SerializeField] int bonoJefe = 2000;

    [Header("Referencias (las asigna el constructor)")]
    [SerializeField] JugadorMovimiento jugador;
    [SerializeField] Transform puntoInicio;
    [SerializeField] SaludEnemigo objetivoFinal; // opcional: punto de control que hay que destruir tras el jefe
    [SerializeField] float tiempoReaparicion = 1.3f;

    public float MultiplicadorVida => multiplicadorVida;
    public float MultiplicadorAgresividad => multiplicadorAgresividad;
    public float MultiplicadorBotiquines => multiplicadorBotiquines;
    public bool JuegoActivo { get; private set; }
    public bool PuedePausar => JuegoActivo;
    public int NumeroNivel => numeroNivel;

    Vector3 puntoReaparicion;
    ArenaJefe arenaActiva;
    bool murioEnEsteNivel;
    bool terminado;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        puntoReaparicion = puntoInicio != null ? puntoInicio.position : jugador.transform.position;
        if (GameManager.Instancia != null) GameManager.Instancia.RegistrarInicioNivel(numeroNivel);
        if (CamaraSeguimiento.Instancia != null) CamaraSeguimiento.Instancia.SaltarAlObjetivo();
        if (AudioManager.Instancia != null) AudioManager.Instancia.ReproducirMusica(musicaNivel, 0.5f);
        StartCoroutine(InicioNivel());
    }

    IEnumerator InicioNivel()
    {
        jugador.ControlBloqueado = true;
        JuegoActivo = false;

        // Intro tipo cómic (solo Misión 1 en partida nueva): el juego queda en pausa mientras se ve
        GameManager gm = GameManager.Instancia;
        if (numeroNivel == 1 && gm != null && gm.IntroPendiente)
        {
            gm.IntroPendiente = false;
            Time.timeScale = 0f;
            yield return IntroComic.Reproducir(gm.PersonajeActual);
            Time.timeScale = 1f;
            yield return null;
        }

        // Briefing (solo nivel 1): pausa el juego hasta que el jugador presione una tecla
        if (!string.IsNullOrEmpty(textoBriefing) && HUD.Instancia != null)
        {
            Time.timeScale = 0f;
            bool listo = false;
            HUD.Instancia.MostrarBriefing(textoBriefing, () => listo = true);
            while (!listo) yield return null;
            Time.timeScale = 1f;
        }

        if (HUD.Instancia != null) HUD.Instancia.MostrarTarjeta(titulo, subtitulo);
        if (AudioManager.Instancia != null) AudioManager.SonarClip(AudioManager.Instancia.jingleInicioNivel, 0.8f, 0f);
        yield return new WaitForSeconds(0.6f);
        jugador.ControlBloqueado = false;
        JuegoActivo = true;
    }

    // ---------- Checkpoints ----------
    public void ActivarCheckpoint(Vector3 posicion)
    {
        puntoReaparicion = posicion;
    }

    // ---------- Muerte del jugador ----------
    public void JugadorMurio()
    {
        if (terminado) return;
        murioEnEsteNivel = true;
        StartCoroutine(RutinaMuerte());
    }

    IEnumerator RutinaMuerte()
    {
        GameManager gm = GameManager.Instancia;
        if (gm != null) gm.PerderVida();
        yield return new WaitForSeconds(tiempoReaparicion);

        if (gm != null && gm.Vidas <= 0)
        {
            JuegoActivo = false;
            terminado = true;
            if (AudioManager.Instancia != null) AudioManager.Instancia.ReproducirJingle(AudioManager.Instancia.jingleGameOver);
            if (PantallaFinal.Instancia != null) PantallaFinal.Instancia.MostrarGameOver(gm.Puntos);
            yield break;
        }

        // Si estábamos peleando contra el jefe, reaparece dentro de la arena
        Vector3 destino = arenaActiva != null ? arenaActiva.PuntoReaparicion : puntoReaparicion;
        jugador.Reaparecer(destino);
        jugador.GetComponent<VidaJugador>().Reaparecer();
        if (CamaraSeguimiento.Instancia != null && arenaActiva == null) CamaraSeguimiento.Instancia.SaltarAlObjetivo();
        if (HUD.Instancia != null) HUD.Instancia.MostrarAviso("¡ADELANTE, SOLDADO!", 1.2f);
    }

    // ---------- Jefe ----------
    public void IniciarJefe(ArenaJefe arena)
    {
        arenaActiva = arena;
        if (AudioManager.Instancia != null) AudioManager.Instancia.ReproducirMusica(AudioManager.Instancia.musicaJefe, 1.5f);
    }

    public void JefeDerrotado()
    {
        if (GameManager.Instancia != null) GameManager.Instancia.SumarPuntos(bonoJefe);

        // Nivel 2: después del tanque hay que destruir el punto de control
        if (objetivoFinal != null && !objetivoFinal.Muerto)
        {
            objetivoFinal.Invulnerable = false;
            objetivoFinal.AlMorir += () => StartCoroutine(CompletarNivel(1.2f));
            if (HUD.Instancia != null)
            {
                HUD.Instancia.MostrarAviso("¡DESTRUYE EL PUNTO DE CONTROL!", 2.5f);
                HUD.Instancia.MostrarJefe(objetivoFinal, "PUNTO DE CONTROL");
            }
            return;
        }
        StartCoroutine(CompletarNivel(2f));
    }

    IEnumerator CompletarNivel(float espera)
    {
        if (terminado) yield break;
        terminado = true;
        yield return new WaitForSeconds(espera);
        JuegoActivo = false;
        jugador.ControlBloqueado = true;
        if (HUD.Instancia != null) HUD.Instancia.OcultarJefe();

        GameManager gm = GameManager.Instancia;
        if (!murioEnEsteNivel && gm != null)
        {
            gm.SumarPuntos(bonoNivelSinMorir);
            if (HUD.Instancia != null) HUD.Instancia.MostrarAviso("¡SIN BAJAS! +" + bonoNivelSinMorir, 2f);
            yield return new WaitForSeconds(1.5f);
        }

        AudioManager am = AudioManager.Instancia;
        if (numeroNivel < 3)
        {
            if (am != null) am.ReproducirJingle(am.jingleVictoria);
            if (HUD.Instancia != null) HUD.Instancia.MostrarAviso("MISIÓN CUMPLIDA", 2.5f);
            jugador.GetComponent<JugadorDisparo>().GuardarArmaEnPartida();
            yield return new WaitForSeconds(3f);
            if (gm != null) gm.SiguienteNivel();
        }
        else
        {
            if (am != null) am.ReproducirJingle(am.jingleVictoria);
            yield return new WaitForSeconds(1.5f);
            if (am != null) am.ReproducirMusica(am.musicaFinal, 1f);
            if (PantallaFinal.Instancia != null) PantallaFinal.Instancia.MostrarVictoria(gm != null ? gm.Puntos : 0, textoFinal);
        }
    }
}
