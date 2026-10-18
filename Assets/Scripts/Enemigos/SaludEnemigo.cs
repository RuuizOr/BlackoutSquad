using System;
using System.Collections;
using UnityEngine;

public enum TipoMuerte { Explota, Cae }

// Vida de enemigos, torretas y jefes: flash al recibir daño, escudo frontal opcional,
// puntos, popup, drop y efecto de muerte (explosión o caída estilizada, sin sangre).
public class SaludEnemigo : MonoBehaviour, IDañable
{
    [SerializeField] int vidaMaxima = 1;
    [SerializeField] int puntos = 100;
    [SerializeField] TipoMuerte tipoMuerte = TipoMuerte.Cae;
    [SerializeField] bool esJefe;
    [SerializeField] bool aplicarDificultad = true;
    [SerializeField] bool invulnerableAlInicio;   // p. ej. el punto de control antes de vencer al tanque

    [Header("Protección frontal (escudo / blindaje)")]
    [Tooltip("0 = sin protección, 1 = bloquea todo lo que llega de frente")]
    [SerializeField, Range(0f, 1f)] float proteccionFrontal = 0f;

    [Header("Feedback")]
    [SerializeField] SpriteRenderer[] renders;
    [SerializeField] Color colorFlash = new Color(1f, 0.45f, 0.45f);
    [SerializeField] float retroceso = 0.06f;

    public int Vida { get; private set; }
    public int VidaMaxima { get; private set; }
    public bool Muerto { get; private set; }
    public bool Invulnerable { get; set; }
    public bool EsJefe => esJefe;

    public event Action AlMorir;
    public event Action AlRecibirDaño;

    Color[] coloresBase;
    float tiempoFlash;
    float tiempoSonidoBloqueo;

    void Awake()
    {
        if (renders == null || renders.Length == 0) renders = GetComponentsInChildren<SpriteRenderer>();
        coloresBase = new Color[renders.Length];
        for (int i = 0; i < renders.Length; i++) coloresBase[i] = renders[i].color;
        VidaMaxima = vidaMaxima;
        Vida = vidaMaxima;
        Invulnerable = invulnerableAlInicio;
    }

    void Start()
    {
        // La dificultad del nivel aumenta la vida de los enemigos normales
        if (aplicarDificultad && !esJefe && ControladorNivel.Instancia != null)
        {
            VidaMaxima = Mathf.CeilToInt(vidaMaxima * ControladorNivel.Instancia.MultiplicadorVida);
            Vida = VidaMaxima;
        }
    }

    // Permite a los jefes cambiar el color base (por ejemplo, fase 2 en rojo)
    public void CambiarColorBase(Color c)
    {
        for (int i = 0; i < renders.Length; i++) { coloresBase[i] = c; renders[i].color = c; }
    }

    public bool RecibirDaño(int cantidad, Vector2 direccion)
    {
        if (Muerto) return false;

        if (Invulnerable)
        {
            Bloqueado();
            return true;
        }

        // Escudo frontal: la bala viaja en sentido contrario a donde mira el enemigo
        float frente = transform.lossyScale.x >= 0f ? 1f : -1f;
        if (proteccionFrontal > 0f && direccion.x * frente < -0.3f)
        {
            cantidad = Mathf.FloorToInt(cantidad * (1f - proteccionFrontal));
            if (cantidad <= 0)
            {
                Bloqueado();
                return true;
            }
        }

        Vida -= cantidad;
        tiempoFlash = 0.08f;
        AlRecibirDaño?.Invoke();
        AudioManager.Sonar(Sfx.DañoEnemigo, 0.5f);
        if (!esJefe && direccion.x != 0f) transform.position += new Vector3(Mathf.Sign(direccion.x) * retroceso, 0f, 0f);

        if (Vida <= 0) Morir();
        return true;
    }

    void Bloqueado()
    {
        if (tiempoSonidoBloqueo > 0f) return;
        tiempoSonidoBloqueo = 0.1f;
        AudioManager.Sonar(Sfx.GolpeEscudo, 0.6f);
    }

    void Update()
    {
        tiempoSonidoBloqueo -= Time.deltaTime;
        if (tiempoFlash > 0f)
        {
            tiempoFlash -= Time.deltaTime;
            for (int i = 0; i < renders.Length; i++)
                if (renders[i] != null) renders[i].color = tiempoFlash > 0f ? colorFlash : coloresBase[i];
        }
    }

    public void Matar()
    {
        if (!Muerto) { Vida = 0; Morir(); }
    }

    void Morir()
    {
        Muerto = true;
        Vida = 0;
        if (GameManager.Instancia != null) GameManager.Instancia.SumarPuntos(puntos);
        Efectos.Popup(transform.position + Vector3.up * 2f, "+" + puntos, new Color(1f, 0.9f, 0.3f));
        AudioManager.Sonar(Sfx.EnemigoDerrotado, 0.8f);

        SoltarObjeto drop = GetComponent<SoltarObjeto>();
        if (drop != null) drop.Soltar();

        AlMorir?.Invoke();
        if (esJefe) return; // el jefe maneja su propia secuencia de muerte

        if (tipoMuerte == TipoMuerte.Explota)
        {
            Efectos.Explosion(transform.position + Vector3.up * 0.6f, false);
            Destroy(gameObject);
        }
        else StartCoroutine(Caer());
    }

    // Caída estilizada: salta hacia atrás, gira, parpadea y desaparece (sin sangre)
    IEnumerator Caer()
    {
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        float lado = transform.lossyScale.x >= 0f ? -1f : 1f;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.linearVelocity = new Vector2(lado * 3f, 7f);
        }
        Efectos.Humo(transform.position + Vector3.up * 0.8f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 80f * -lado, t * 2f));
            bool visible = t < 0.5f || Mathf.FloorToInt(t * 20f) % 2 == 0;
            foreach (SpriteRenderer r in renders) if (r != null) r.enabled = visible;
            yield return null;
        }
        Destroy(gameObject);
    }
}
