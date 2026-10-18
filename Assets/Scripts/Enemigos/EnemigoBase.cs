using UnityEngine;

// Funciones comunes de todos los enemigos: ver al jugador, voltear, disparar.
// Cada enemigo concreto escribe su propia máquina de estados en ActualizarIA().
[RequireComponent(typeof(SaludEnemigo))]
public abstract class EnemigoBase : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] protected AnimadorSprites animador;
    [SerializeField] protected Transform bocaDisparo;

    [Header("Disparo")]
    [SerializeField] protected GameObject prefabBala;
    [SerializeField] protected int dañoBala = 20;
    [SerializeField] protected float velocidadBala = 9f;
    [SerializeField] protected float vidaBala = 3f;

    [Header("Percepción")]
    [SerializeField] protected float rangoVision = 9f;
    [SerializeField] protected float distanciaActivacion = 13f;

    protected SaludEnemigo salud;
    protected Rigidbody2D rb;
    float tiempoAnimDisparo;

    protected bool MirandoDerecha => transform.localScale.x > 0f;
    protected float Lado => MirandoDerecha ? 1f : -1f;

    // Multiplicador de agresividad del nivel (dispara más seguido en niveles altos)
    protected float Agresividad => ControladorNivel.Instancia != null ? ControladorNivel.Instancia.MultiplicadorAgresividad : 1f;

    protected Transform Jugador
    {
        get
        {
            JugadorMovimiento j = JugadorMovimiento.Instancia;
            return (j != null && !j.EstaMuerto) ? j.transform : null;
        }
    }

    protected virtual void Awake()
    {
        salud = GetComponent<SaludEnemigo>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        if (salud.Muerto) return;
        if (tiempoAnimDisparo > 0f) tiempoAnimDisparo -= Time.deltaTime;
        if (!CercaDeCamara()) { Detener(); return; } // fuera de pantalla no hace nada
        ActualizarIA();
    }

    protected abstract void ActualizarIA();

    protected bool CercaDeCamara()
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        return Mathf.Abs(transform.position.x - cam.transform.position.x) < distanciaActivacion;
    }

    protected void Detener()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected float DistanciaAlJugador()
    {
        Transform j = Jugador;
        return j == null ? float.MaxValue : Vector2.Distance(transform.position, j.position);
    }

    // ¿El jugador está en rango y sin paredes de por medio?
    protected bool JugadorVisible(float rango)
    {
        Transform j = Jugador;
        if (j == null) return false;
        Vector2 ojos = (Vector2)transform.position + Vector2.up * 1.2f;
        Vector2 objetivo = (Vector2)j.position + Vector2.up * 1f;
        if (Vector2.Distance(ojos, objetivo) > rango) return false;
        if (Mathf.Abs(objetivo.y - ojos.y) > 4f) return false;
        return Physics2D.Linecast(ojos, objetivo, Capas.MascaraSuelo).collider == null;
    }

    protected void MirarHacia(float x)
    {
        bool derecha = x > transform.position.x;
        Vector3 e = transform.localScale;
        e.x = Mathf.Abs(e.x) * (derecha ? 1f : -1f);
        transform.localScale = e;
    }

    protected void MirarAlJugador()
    {
        if (Jugador != null) MirarHacia(Jugador.position.x);
    }

    // Dispara una bala desde la boca en la dirección indicada
    protected void DispararBala(Vector2 direccion, GameObject prefab = null, float escala = 1f)
    {
        GameObject p = prefab != null ? prefab : prefabBala;
        if (p == null) return;
        Vector3 origen = bocaDisparo != null ? bocaDisparo.position : transform.position + Vector3.up;
        GameObject b = PoolObjetos.Sacar(p, origen, Quaternion.identity);
        b.GetComponent<Bala>().Disparar(direccion, velocidadBala, dañoBala, vidaBala);
        b.transform.localScale = p.transform.localScale * escala;
        Efectos.Fogonazo(origen, direccion);
        AudioManager.Sonar(Sfx.DisparoEnemigo, 0.55f);
        tiempoAnimDisparo = 0.15f;
    }

    protected Vector2 DireccionAlJugador()
    {
        Transform j = Jugador;
        if (j == null) return new Vector2(Lado, 0f);
        Vector3 origen = bocaDisparo != null ? bocaDisparo.position : transform.position;
        return ((Vector2)(j.position + Vector3.up * 1f - origen)).normalized;
    }

    // Elige la animación (muestra "Disparo" un instante tras disparar)
    protected void Animar(string clipNormal)
    {
        if (animador == null) return;
        if (tiempoAnimDisparo > 0f && animador.TieneClip("Disparo")) animador.Reproducir("Disparo");
        else animador.Reproducir(clipNormal);
    }
}
