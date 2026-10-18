using UnityEngine;

// Cámara lateral propia: sigue al jugador con suavizado, se adelanta hacia donde mira,
// respeta los límites del nivel, se puede bloquear en la arena del jefe y sabe sacudirse.
public class CamaraSeguimiento : MonoBehaviour
{
    public static CamaraSeguimiento Instancia { get; private set; }

    [SerializeField] Transform objetivo;
    [SerializeField] float suavizadoX = 0.12f;
    [SerializeField] float suavizadoY = 0.25f;
    [SerializeField] float adelanto = 2.2f;
    [SerializeField] float alturaSobreJugador = 1.5f;

    [Header("Límites del nivel (los asigna el constructor)")]
    [SerializeField] float limiteIzquierdo = 0f;
    [SerializeField] float limiteDerecho = 200f;
    [SerializeField] float limiteInferior = 0f;
    [SerializeField] float limiteSuperior = 16f;

    Camera cam;
    Vector3 velocidad;
    float adelantoActual;
    bool bloqueada;
    float bloqueoMin, bloqueoMax;

    Vector3 posicionBase;
    bool iniciada;

    // Sacudida
    float intensidadSacudida, tiempoSacudida, duracionSacudida;

    void Awake()
    {
        Instancia = this;
        cam = GetComponent<Camera>();
    }

    public void AsignarObjetivo(Transform t) { objetivo = t; }

    public void ConfigurarLimites(float izq, float der, float inf, float sup)
    {
        limiteIzquierdo = izq; limiteDerecho = der; limiteInferior = inf; limiteSuperior = sup;
    }

    // Fija la cámara entre dos X (arena del jefe)
    public void Bloquear(float minX, float maxX)
    {
        bloqueada = true;
        bloqueoMin = minX;
        bloqueoMax = maxX;
    }

    public void Desbloquear() { bloqueada = false; }

    public void SaltarAlObjetivo()
    {
        if (objetivo == null) return;
        posicionBase = CalcularDestino();
        transform.position = posicionBase;
        velocidad = Vector3.zero;
        iniciada = true;
    }

    public static void Sacudir(float intensidad, float duracion)
    {
        if (Instancia == null) return;
        if (intensidad < Instancia.intensidadSacudida && Instancia.tiempoSacudida > 0f) return;
        Instancia.intensidadSacudida = intensidad;
        Instancia.duracionSacudida = duracion;
        Instancia.tiempoSacudida = duracion;
    }

    Vector3 CalcularDestino()
    {
        float mitadAlto = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;

        float lado = objetivo.lossyScale.x >= 0f ? 1f : -1f;
        adelantoActual = Mathf.MoveTowards(adelantoActual, adelanto * lado, 6f * Time.deltaTime);

        float x = objetivo.position.x + adelantoActual;
        float y = objetivo.position.y + alturaSobreJugador;

        float minX = bloqueada ? bloqueoMin : limiteIzquierdo;
        float maxX = bloqueada ? bloqueoMax : limiteDerecho;
        x = Mathf.Clamp(x, minX + mitadAncho, Mathf.Max(minX + mitadAncho, maxX - mitadAncho));
        y = Mathf.Clamp(y, limiteInferior + mitadAlto, Mathf.Max(limiteInferior + mitadAlto, limiteSuperior - mitadAlto));
        return new Vector3(x, y, transform.position.z);
    }

    void LateUpdate()
    {
        if (objetivo == null) return;
        if (!iniciada) { posicionBase = transform.position; iniciada = true; }

        Vector3 destino = CalcularDestino();
        posicionBase.x = Mathf.SmoothDamp(posicionBase.x, destino.x, ref velocidad.x, suavizadoX);
        posicionBase.y = Mathf.SmoothDamp(posicionBase.y, destino.y, ref velocidad.y, suavizadoY);
        posicionBase.z = transform.position.z;

        // La sacudida se suma encima de la posición base (así no se acumula)
        Vector3 desplazamiento = Vector3.zero;
        if (tiempoSacudida > 0f)
        {
            tiempoSacudida -= Time.unscaledDeltaTime;
            float fuerza = intensidadSacudida * (tiempoSacudida / Mathf.Max(0.01f, duracionSacudida));
            desplazamiento = (Vector3)(Random.insideUnitCircle * fuerza);
            if (tiempoSacudida <= 0f) intensidadSacudida = 0f;
        }
        transform.position = posicionBase + desplazamiento;
    }

    public float BordeIzquierdo => transform.position.x - cam.orthographicSize * cam.aspect;
    public float BordeDerecho => transform.position.x + cam.orthographicSize * cam.aspect;
}
