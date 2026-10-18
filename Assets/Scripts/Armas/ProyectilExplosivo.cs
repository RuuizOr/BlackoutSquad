using UnityEngine;

// Proyectil con física que vuela en arco y explota (granada del jugador o mortero de los jefes).
[RequireComponent(typeof(Rigidbody2D))]
public class ProyectilExplosivo : MonoBehaviour
{
    [SerializeField] float tiempoMaximo = 1.6f;
    [SerializeField] float radio = 2.2f;
    [SerializeField] int daño = 8;
    [SerializeField] bool explosionGrande = true;
    [SerializeField] LayerMask capasQueDaña;
    [SerializeField] LayerMask capasQueDetonan;   // tocar estas capas lo hace explotar al instante
    [SerializeField] bool explotarAlTocarSuelo;
    [SerializeField] float giro = 540f;

    Rigidbody2D rb;
    float tiempo;
    bool explotado;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Lanzar(Vector2 velocidad)
    {
        tiempo = 0f;
        explotado = false;
        rb.linearVelocity = velocidad;
        rb.angularVelocity = -Mathf.Sign(velocidad.x) * giro;
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        if (tiempo >= tiempoMaximo) Explotar();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        int capa = col.gameObject.layer;
        if ((capasQueDetonan.value & (1 << capa)) != 0) Explotar();
        else if (explotarAlTocarSuelo && capa == Capas.Suelo) Explotar();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if ((capasQueDetonan.value & (1 << otro.gameObject.layer)) != 0) Explotar();
    }

    void Explotar()
    {
        if (explotado) return;
        explotado = true;
        Efectos.Explosion(transform.position, explosionGrande);
        DañoArea.Aplicar(transform.position, radio, daño, capasQueDaña.value);
        PoolObjetos.Devolver(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
