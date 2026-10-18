using System.Collections;
using UnityEngine;

// Base de todos los jefes: espera a que la arena lo active, es invulnerable antes,
// y al morir hace una secuencia de explosiones, sacudida y "hit stop".
public abstract class JefeBase : EnemigoBase
{
    [Header("Jefe")]
    [SerializeField] string nombreJefe = "JEFE";
    [SerializeField] int explosionesAlMorir = 7;

    protected bool activo;
    protected float arenaMinX = -9999f, arenaMaxX = 9999f;

    public string NombreJefe => nombreJefe;
    public SaludEnemigo Salud => salud;

    protected override void Awake()
    {
        base.Awake();
        salud.AlMorir += AlMorirJefe;
    }

    protected virtual void Start()
    {
        // Invulnerable hasta que la arena lo active
        if (!activo) salud.Invulnerable = true;
    }

    public void ConfigurarArena(float minX, float maxX)
    {
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    public virtual void Activar()
    {
        activo = true;
        salud.Invulnerable = false;
    }

    protected override void Update()
    {
        if (!activo || salud.Muerto) return;
        if (Jugador == null) { Detener(); Animar("Idle"); return; }
        ActualizarIA();
    }

    void AlMorirJefe()
    {
        StopAllCoroutines();
        Detener();
        StartCoroutine(SecuenciaMuerte());
    }

    IEnumerator SecuenciaMuerte()
    {
        if (GameManager.Instancia != null) GameManager.Instancia.HitStop(0.35f);
        Bounds b = new Bounds(transform.position + Vector3.up, Vector3.one * 2f);
        Collider2D col = GetComponentInChildren<Collider2D>();
        if (col != null) b = col.bounds;

        for (int i = 0; i < explosionesAlMorir; i++)
        {
            Vector3 p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
            Efectos.Explosion(p, i % 2 == 0);
            yield return new WaitForSeconds(0.18f);
        }
        Efectos.Explosion(b.center, true);
        CamaraSeguimiento.Sacudir(0.6f, 0.8f);
        if (GameManager.Instancia != null) GameManager.Instancia.HitStop(0.2f);
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        yield return new WaitForSeconds(0.3f);
        if (ControladorNivel.Instancia != null) ControladorNivel.Instancia.JefeDerrotado();
        Destroy(gameObject, 0.1f);
    }

    // Lanza un proyectil en arco que cae cerca de una X (morteros)
    protected void LanzarMortero(GameObject prefab, Vector3 origen, float xObjetivo, float tiempoVuelo)
    {
        if (prefab == null) return;
        GameObject m = PoolObjetos.Sacar(prefab, origen, Quaternion.identity);
        Rigidbody2D mrb = m.GetComponent<Rigidbody2D>();
        float g = Mathf.Abs(Physics2D.gravity.y) * mrb.gravityScale;
        float sueloY = transform.position.y;
        float vx = (xObjetivo - origen.x) / tiempoVuelo;
        float vy = (sueloY - origen.y + 0.5f * g * tiempoVuelo * tiempoVuelo) / tiempoVuelo;
        m.GetComponent<ProyectilExplosivo>().Lanzar(new Vector2(vx, vy));
        AudioManager.Sonar(Sfx.Misil, 0.6f);
    }

    // Daño por contacto (embestidas)
    protected void DañoPorContacto(Vector2 centro, Vector2 tamaño, int daño)
    {
        Collider2D c = Physics2D.OverlapBox(centro, tamaño, 0f, Capas.MascaraJugador);
        if (c == null) return;
        VidaJugador v = c.GetComponentInParent<VidaJugador>();
        if (v != null) v.RecibirDaño(daño, new Vector2(Mathf.Sign(c.transform.position.x - transform.position.x), 0f));
    }
}
