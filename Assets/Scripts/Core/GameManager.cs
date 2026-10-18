using System.Collections;
using UnityEngine;

// Guarda el estado de la partida en curso (vidas, puntos, nivel, arma) y cambia de escena.
// Persiste entre escenas. No guarda nada en disco (solo los volúmenes los guarda el AudioManager).
public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Partida")]
    [SerializeField] int vidasIniciales = 3;
    [SerializeField] int granadasPorVida = 5;
    [SerializeField] int totalNiveles = 3;

    [Header("Escenas")]
    [SerializeField] string escenaMenu = "MainMenu";
    [SerializeField] string prefijoNivel = "Nivel";

    public int Vidas { get; private set; }
    public int Puntos { get; private set; }
    public int NivelActual { get; set; } = 1;
    public int GranadasPorVida => granadasPorVida;
    public int VidasIniciales => vidasIniciales;

    // Arma que se conserva entre niveles durante la partida
    public ArmaDatos ArmaGuardada { get; set; }
    public int MunicionGuardada { get; set; }

    public bool Pausado { get; set; }

    int puntosAlIniciarNivel;
    Coroutine rutinaHitStop;

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
        Vidas = vidasIniciales;
    }

    // ---------- Flujo de partida ----------
    public void NuevaPartida()
    {
        Vidas = vidasIniciales;
        Puntos = 0;
        ArmaGuardada = null;
        MunicionGuardada = 0;
        CargarNivel(1);
    }

    public void CargarNivel(int numero)
    {
        NivelActual = Mathf.Clamp(numero, 1, totalNiveles);
        puntosAlIniciarNivel = Puntos;
        Transicion.Cargar(prefijoNivel + NivelActual);
    }

    // Reinicia el nivel actual con todas las vidas y la puntuación con la que se empezó el nivel.
    public void ReiniciarNivel()
    {
        Vidas = vidasIniciales;
        Puntos = puntosAlIniciarNivel;
        ArmaGuardada = null;
        Transicion.Cargar(prefijoNivel + NivelActual);
    }

    public void SiguienteNivel()
    {
        if (NivelActual < totalNiveles) CargarNivel(NivelActual + 1);
        else IrAlMenu();
    }

    public void IrAlMenu()
    {
        Pausado = false;
        Transicion.Cargar(escenaMenu);
    }

    // Se llama al abrir un nivel directo desde el editor (para probar sin pasar por el menú).
    public void RegistrarInicioNivel(int numero)
    {
        NivelActual = numero;
        puntosAlIniciarNivel = Puntos;
        if (Vidas <= 0) Vidas = vidasIniciales;
    }

    // ---------- Puntos y vidas ----------
    public void SumarPuntos(int cantidad)
    {
        Puntos += cantidad;
    }

    public void PerderVida()
    {
        Vidas = Mathf.Max(0, Vidas - 1);
    }

    public void GanarVida()
    {
        Vidas++;
    }

    // ---------- Hit stop: congela el tiempo un instante para dar "peso" a un golpe ----------
    public void HitStop(float duracion, float escala = 0.05f)
    {
        if (rutinaHitStop != null) StopCoroutine(rutinaHitStop);
        rutinaHitStop = StartCoroutine(RutinaHitStop(duracion, escala));
    }

    IEnumerator RutinaHitStop(float duracion, float escala)
    {
        if (!Pausado) Time.timeScale = escala;
        yield return new WaitForSecondsRealtime(duracion);
        if (!Pausado) Time.timeScale = 1f;
        rutinaHitStop = null;
    }
}
