using UnityEngine;

// Dirección de apuntado del jugador (8 direcciones)
public enum Apuntado { Frente, Arriba, DiagonalArriba, Abajo, DiagonalAbajo, Agachado }

// Disparo en 8 direcciones, armas con munición limitada y granadas.
public class JugadorDisparo : MonoBehaviour
{
    [Header("Armas")]
    [SerializeField] ArmaDatos armaBasica;
    [SerializeField] Color colorPopupArma = new Color(1f, 0.75f, 0.2f);

    [Header("Granadas")]
    [SerializeField] GameObject prefabGranada;
    [SerializeField] float enfriamientoGranada = 1.5f;
    [SerializeField] Vector2 velocidadGranada = new Vector2(6.5f, 7f);
    [SerializeField] Vector2 puntoLanzamiento = new Vector2(0.3f, 1.4f);

    [Header("Punto de disparo (muzzle) según dirección, relativo a los pies")]
    [SerializeField] Vector2 bocaFrente = new Vector2(0.72f, 1.07f);
    [SerializeField] Vector2 bocaAgachado = new Vector2(0.72f, 0.72f);
    [SerializeField] Vector2 bocaArriba = new Vector2(-0.35f, 2.45f);
    [SerializeField] Vector2 bocaDiagonalArriba = new Vector2(0.1f, 2.2f);
    [SerializeField] Vector2 bocaAbajo = new Vector2(0.15f, 0.2f);
    [SerializeField] Vector2 bocaDiagonalAbajo = new Vector2(0.6f, 0.45f);
    [SerializeField] Vector2 bocaAire = new Vector2(0.65f, 1.0f);

    JugadorMovimiento movimiento;
    ArmaDatos armaActual;
    float contadorCadencia, contadorGranada, contadorSonido;

    public ArmaDatos ArmaActual => armaActual;
    public int Municion { get; private set; }
    public int Granadas { get; private set; }
    public Apuntado DireccionApuntado { get; private set; }
    public bool DisparoReciente => tiempoDesdeDisparo < 0.18f;
    float tiempoDesdeDisparo = 99f;

    void Awake()
    {
        movimiento = GetComponent<JugadorMovimiento>();
        armaActual = armaBasica;
    }

    void Start()
    {
        // Recupera el arma de la partida (se conserva entre niveles)
        GameManager gm = GameManager.Instancia;
        if (gm != null && gm.ArmaGuardada != null && gm.MunicionGuardada > 0)
        {
            armaActual = gm.ArmaGuardada;
            Municion = gm.MunicionGuardada;
        }
        if (gm != null && gm.PersonajeActual != null) UsarBocasDe(gm.PersonajeActual);
        RecargarGranadas();
    }

    // Las balas deben salir de la punta del arma de cada comando
    void UsarBocasDe(PersonajeDatos p)
    {
        bocaFrente = p.bocaFrente;
        bocaAgachado = p.bocaAgachado;
        bocaAire = p.bocaAire;
        bocaArriba = p.bocaArriba;
        bocaDiagonalArriba = p.bocaArriba + new Vector2(0.45f, -0.25f);
    }

    public void RecargarGranadas()
    {
        Granadas = GameManager.Instancia != null ? GameManager.Instancia.GranadasPorVida : 5;
    }

    public void SumarGranadas(int cantidad) { Granadas += cantidad; }

    void Update()
    {
        tiempoDesdeDisparo += Time.deltaTime;
        contadorCadencia -= Time.deltaTime;
        contadorGranada -= Time.deltaTime;
        contadorSonido -= Time.deltaTime;
        if (movimiento.EstaMuerto || movimiento.ControlBloqueado) return;

        DireccionApuntado = CalcularApuntado();

        if (movimiento.EstaRodando) return;

        if (ControlesJuego.DisparoMantenido && contadorCadencia <= 0f) Disparar();
        if (ControlesJuego.GranadaPresionada) LanzarGranada();
    }

    Apuntado CalcularApuntado()
    {
        int h = ControlesJuego.Horizontal;
        int v = ControlesJuego.Vertical;
        if (movimiento.Estado == EstadoJugador.Agachado) return Apuntado.Agachado;
        if (v > 0) return h != 0 ? Apuntado.DiagonalArriba : Apuntado.Arriba;
        if (v < 0 && !movimiento.EnSuelo) return h != 0 ? Apuntado.DiagonalAbajo : Apuntado.Abajo;
        return Apuntado.Frente;
    }

    // Vector de disparo en el mundo según el apuntado y hacia dónde mira
    public Vector2 VectorDisparo()
    {
        float lado = movimiento.MirandoDerecha ? 1f : -1f;
        switch (DireccionApuntado)
        {
            case Apuntado.Arriba: return Vector2.up;
            case Apuntado.DiagonalArriba: return new Vector2(lado, 1f).normalized;
            case Apuntado.Abajo: return Vector2.down;
            case Apuntado.DiagonalAbajo: return new Vector2(lado, -1f).normalized;
            default: return new Vector2(lado, 0f);
        }
    }

    public Vector3 PuntoDisparo()
    {
        Vector2 local;
        switch (DireccionApuntado)
        {
            case Apuntado.Arriba: local = bocaArriba; break;
            case Apuntado.DiagonalArriba: local = bocaDiagonalArriba; break;
            case Apuntado.Abajo: local = bocaAbajo; break;
            case Apuntado.DiagonalAbajo: local = bocaDiagonalAbajo; break;
            case Apuntado.Agachado: local = bocaAgachado; break;
            default: local = movimiento.EnSuelo ? bocaFrente : bocaAire; break;
        }
        float lado = movimiento.MirandoDerecha ? 1f : -1f;
        return transform.position + new Vector3(local.x * lado, local.y, 0f);
    }

    void Disparar()
    {
        ArmaDatos arma = armaActual != null ? armaActual : armaBasica;
        if (arma == null || arma.prefabBala == null) return;
        contadorCadencia = arma.cadencia;
        tiempoDesdeDisparo = 0f;

        Vector2 dir = VectorDisparo();
        Vector3 boca = PuntoDisparo();
        int n = Mathf.Max(1, arma.proyectilesPorDisparo);
        for (int i = 0; i < n; i++)
        {
            float angulo = 0f;
            if (n > 1) angulo = Mathf.Lerp(-arma.dispersionGrados, arma.dispersionGrados, i / (float)(n - 1));
            else if (arma.dispersionGrados > 0f) angulo = Random.Range(-arma.dispersionGrados, arma.dispersionGrados);
            Vector2 d = Quaternion.Euler(0f, 0f, angulo) * dir;
            float velocidad = arma.velocidadBala * (n > 1 ? Random.Range(0.85f, 1.1f) : 1f);
            GameObject b = PoolObjetos.Sacar(arma.prefabBala, boca, Quaternion.identity);
            b.GetComponent<Bala>().Disparar(d, velocidad, arma.daño, arma.vidaBala);
        }

        if (arma.muestraFogonazo) Efectos.Fogonazo(boca, dir);
        if (arma.expulsaCasquillo) Efectos.Casquillo(transform.position + Vector3.up * 1.1f, movimiento.MirandoDerecha ? 1f : -1f);
        if (contadorSonido <= 0f)
        {
            AudioManager.SonarClip(arma.sonidoDisparo, arma.volumenSonido);
            contadorSonido = arma.intervaloSonido;
        }

        // Munición limitada: al agotarse se vuelve a la pistola
        if (!arma.EsInfinita && arma != armaBasica)
        {
            Municion--;
            if (Municion <= 0)
            {
                armaActual = armaBasica;
                AudioManager.Sonar(Sfx.SinMunicion);
                Efectos.Popup(transform.position + Vector3.up * 2.4f, "SIN MUNICIÓN", new Color(1f, 0.4f, 0.3f));
            }
        }
    }

    void LanzarGranada()
    {
        if (contadorGranada > 0f || Granadas <= 0 || prefabGranada == null) return;
        contadorGranada = enfriamientoGranada;
        Granadas--;
        float lado = movimiento.MirandoDerecha ? 1f : -1f;
        Vector3 origen = transform.position + new Vector3(puntoLanzamiento.x * lado, puntoLanzamiento.y, 0f);
        GameObject g = PoolObjetos.Sacar(prefabGranada, origen, Quaternion.identity);
        Vector2 vel = new Vector2(velocidadGranada.x * lado + movimiento.VelocidadHorizontal * 0.4f, velocidadGranada.y);
        g.GetComponent<ProyectilExplosivo>().Lanzar(vel);
        AudioManager.Sonar(Sfx.LanzarGranada, 0.6f);
    }

    public void EquiparArma(ArmaDatos arma)
    {
        if (arma == null) return;
        armaActual = arma;
        Municion = arma.municion;
    }

    // Al morir se pierde el arma especial
    public void PerderArmaEspecial()
    {
        armaActual = armaBasica;
        Municion = 0;
    }

    // Se llama al terminar el nivel para conservar el arma en el siguiente
    public void GuardarArmaEnPartida()
    {
        if (GameManager.Instancia == null) return;
        bool especial = armaActual != null && armaActual != armaBasica && Municion > 0;
        GameManager.Instancia.ArmaGuardada = especial ? armaActual : null;
        GameManager.Instancia.MunicionGuardada = especial ? Municion : 0;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float lado = transform.localScale.x >= 0f ? 1f : -1f;
        foreach (Vector2 p in new[] { bocaFrente, bocaAgachado, bocaArriba, bocaDiagonalArriba, bocaAbajo, bocaDiagonalAbajo })
            Gizmos.DrawWireSphere(transform.position + new Vector3(p.x * lado, p.y, 0f), 0.06f);
    }
}
