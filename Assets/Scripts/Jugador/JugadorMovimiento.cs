using UnityEngine;

public enum EstadoJugador { EnSuelo, EnAire, Agachado, Rodando, Muerto }

// Movimiento del Comando Alfa: caminar, saltar (coyote time, buffer y altura variable),
// agacharse y rodar. Máquina de estados con enum + switch.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class JugadorMovimiento : MonoBehaviour
{
    public static JugadorMovimiento Instancia { get; private set; }

    [Header("Caminar")]
    [SerializeField] float velocidad = 5.5f;
    [SerializeField] float aceleracion = 60f;

    [Header("Salto")]
    [SerializeField] float fuerzaSalto = 14.5f;
    [SerializeField] float tiempoCoyote = 0.1f;
    [SerializeField] float tiempoBufferSalto = 0.12f;
    [SerializeField] float multiplicadorCorteSalto = 0.45f;
    [SerializeField] float gravedadNormal = 3.2f;
    [SerializeField] float gravedadCaida = 4.6f;
    [SerializeField] float velocidadMaximaCaida = 18f;

    [Header("Rodar (esquivar)")]
    [SerializeField] float velocidadRodar = 9f;
    [SerializeField] float duracionRodar = 0.4f;
    [SerializeField] float invulnerabilidadRodar = 0.2f;
    [SerializeField] float enfriamientoRodar = 1f;
    [SerializeField] Transform visual; // se gira mientras rueda

    [Header("Detección de suelo")]
    [SerializeField] Vector2 tamañoChequeoSuelo = new Vector2(0.55f, 0.12f);
    [SerializeField] LayerMask capasSuelo;

    [Header("Colisionador")]
    [SerializeField] Vector2 tamañoDePie = new Vector2(0.6f, 1.65f);
    [SerializeField] Vector2 tamañoAgachado = new Vector2(0.6f, 0.98f);

    Rigidbody2D rb;
    BoxCollider2D caja;

    public EstadoJugador Estado { get; private set; } = EstadoJugador.EnSuelo;
    public bool MirandoDerecha { get; private set; } = true;
    public bool EnSuelo { get; private set; }
    public bool EstaMuerto => Estado == EstadoJugador.Muerto;
    public bool EstaRodando => Estado == EstadoJugador.Rodando;
    public bool InvulnerablePorRodar => Estado == EstadoJugador.Rodando && tiempoRodando < invulnerabilidadRodar;
    public float VelocidadHorizontal => rb.linearVelocity.x;
    public bool ControlBloqueado { get; set; }

    float contadorCoyote, contadorBuffer, contadorEnfriamientoRodar, tiempoRodando, tiempoEmpuje;
    float direccionRodar;
    int entradaH;
    float tiempoPaso;
    bool estabaEnSuelo = true;

    void Awake()
    {
        Instancia = this;
        rb = GetComponent<Rigidbody2D>();
        caja = GetComponent<BoxCollider2D>();
        rb.gravityScale = gravedadNormal;
    }

    void Update()
    {
        if (Estado == EstadoJugador.Muerto) return;

        EnSuelo = ChequearSuelo();
        entradaH = ControlBloqueado ? 0 : ControlesJuego.Horizontal;
        int entradaV = ControlBloqueado ? 0 : ControlesJuego.Vertical;

        // Coyote time y buffer de salto
        contadorCoyote = EnSuelo ? tiempoCoyote : contadorCoyote - Time.deltaTime;
        contadorBuffer = (!ControlBloqueado && ControlesJuego.SaltoPresionado) ? tiempoBufferSalto : contadorBuffer - Time.deltaTime;
        contadorEnfriamientoRodar -= Time.deltaTime;
        tiempoEmpuje -= Time.deltaTime;

        // Aterrizaje
        if (EnSuelo && !estabaEnSuelo && rb.linearVelocity.y <= 0.1f)
        {
            Efectos.Polvo(transform.position);
            AudioManager.Sonar(Sfx.Aterrizaje, 0.7f);
        }
        estabaEnSuelo = EnSuelo;

        switch (Estado)
        {
            case EstadoJugador.EnSuelo:
            case EstadoJugador.EnAire:
            case EstadoJugador.Agachado:
                ActualizarNormal(entradaV);
                break;
            case EstadoJugador.Rodando:
                ActualizarRodando();
                break;
        }
    }

    void ActualizarNormal(int entradaV)
    {
        // Voltear hacia donde camina
        if (entradaH != 0) Mirar(entradaH > 0);

        // Estado según el suelo y la tecla de agacharse
        if (EnSuelo) Estado = (entradaV < 0) ? EstadoJugador.Agachado : EstadoJugador.EnSuelo;
        else Estado = EstadoJugador.EnAire;
        AjustarColisionador(Estado == EstadoJugador.Agachado);

        // Saltar (usa coyote y buffer)
        if (contadorBuffer > 0f && contadorCoyote > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            contadorBuffer = 0f;
            contadorCoyote = 0f;
            Estado = EstadoJugador.EnAire;
            AjustarColisionador(false);
            Efectos.Polvo(transform.position);
            AudioManager.Sonar(Sfx.Salto, 0.8f);
        }

        // Salto de altura variable: soltar la tecla corta el ascenso
        if (!ControlesJuego.SaltoMantenido && rb.linearVelocity.y > 0f && Estado == EstadoJugador.EnAire)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * multiplicadorCorteSalto);

        // Rodar (solo en el suelo)
        if (!ControlBloqueado && ControlesJuego.RodarPresionado && EnSuelo && contadorEnfriamientoRodar <= 0f)
            EmpezarRodar();

        // Pasos
        if (Estado == EstadoJugador.EnSuelo && entradaH != 0)
        {
            tiempoPaso -= Time.deltaTime;
            if (tiempoPaso <= 0f)
            {
                tiempoPaso = 0.32f;
                AudioManager.Sonar(Sfx.Paso, 0.35f);
                Efectos.Polvo(transform.position);
            }
        }
    }

    void EmpezarRodar()
    {
        Estado = EstadoJugador.Rodando;
        tiempoRodando = 0f;
        contadorEnfriamientoRodar = enfriamientoRodar;
        direccionRodar = entradaH != 0 ? entradaH : (MirandoDerecha ? 1f : -1f);
        Mirar(direccionRodar > 0f);
        AjustarColisionador(true);
        AudioManager.Sonar(Sfx.Rodar);
        Efectos.Polvo(transform.position);
    }

    void ActualizarRodando()
    {
        tiempoRodando += Time.deltaTime;
        float progreso = tiempoRodando / duracionRodar;
        if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, -360f * progreso);
        if (tiempoRodando >= duracionRodar)
        {
            if (visual != null) visual.localRotation = Quaternion.identity;
            Estado = EnSuelo ? EstadoJugador.EnSuelo : EstadoJugador.EnAire;
            AjustarColisionador(false);
        }
    }

    void FixedUpdate()
    {
        if (Estado == EstadoJugador.Muerto) return;

        Vector2 v = rb.linearVelocity;
        if (Estado == EstadoJugador.Rodando)
        {
            v.x = direccionRodar * velocidadRodar;
        }
        else if (tiempoEmpuje <= 0f)
        {
            float objetivo = (Estado == EstadoJugador.Agachado) ? 0f : entradaH * velocidad;
            v.x = Mathf.MoveTowards(v.x, objetivo, aceleracion * Time.fixedDeltaTime);
        }

        // Caída más pesada para un salto con buena sensación
        rb.gravityScale = v.y < 0f ? gravedadCaida : gravedadNormal;
        v.y = Mathf.Max(v.y, -velocidadMaximaCaida);
        rb.linearVelocity = v;
    }

    bool ChequearSuelo()
    {
        if (rb.linearVelocity.y > 0.5f) return false;
        Vector2 centro = (Vector2)transform.position + Vector2.down * 0.02f;
        return Physics2D.OverlapBox(centro, tamañoChequeoSuelo, 0f, capasSuelo) != null;
    }

    void AjustarColisionador(bool bajo)
    {
        Vector2 t = bajo ? tamañoAgachado : tamañoDePie;
        caja.size = t;
        caja.offset = new Vector2(0f, t.y * 0.5f);
    }

    public void Mirar(bool derecha)
    {
        MirandoDerecha = derecha;
        Vector3 e = transform.localScale;
        e.x = Mathf.Abs(e.x) * (derecha ? 1f : -1f);
        transform.localScale = e;
    }

    // Retroceso al recibir daño
    public void Empujar(Vector2 fuerza)
    {
        if (Estado == EstadoJugador.Rodando || Estado == EstadoJugador.Muerto) return;
        rb.linearVelocity = fuerza;
        tiempoEmpuje = 0.15f;
    }

    public void Morir()
    {
        Estado = EstadoJugador.Muerto;
        if (visual != null) visual.localRotation = Quaternion.identity;
        rb.linearVelocity = new Vector2(MirandoDerecha ? -3f : 3f, 6f);
        AjustarColisionador(true);
    }

    public void Reaparecer(Vector3 posicion)
    {
        transform.position = posicion;
        rb.linearVelocity = Vector2.zero;
        Estado = EstadoJugador.EnAire;
        Mirar(true);
        AjustarColisionador(false);
        ControlBloqueado = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.down * 0.02f, tamañoChequeoSuelo);
    }
}
