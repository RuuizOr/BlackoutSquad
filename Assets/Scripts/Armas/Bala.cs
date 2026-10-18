using System.Collections.Generic;
using UnityEngine;

// Proyectil recto (jugador o enemigo). La capa del objeto decide a quién puede golpear
// (BalaJugador choca con Enemigo y Suelo; BalaEnemigo con Jugador y Suelo).
[RequireComponent(typeof(Rigidbody2D))]
public class Bala : MonoBehaviour
{
    [SerializeField] bool atraviesa;            // el lanzallamas atraviesa enemigos
    [SerializeField] bool chispasAlImpactar = true;
    [SerializeField] float crecimiento = 0f;    // la llama crece mientras viaja
    [SerializeField] SpriteRenderer render;

    Rigidbody2D rb;
    Vector2 direccion;
    int daño;
    float vida, vidaTotal;
    Vector3 escalaInicial;
    readonly HashSet<IDañable> golpeados = new HashSet<IDañable>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        escalaInicial = transform.localScale;
    }

    public void Disparar(Vector2 dir, float velocidad, int dañoBala, float segundosVida)
    {
        direccion = dir.normalized;
        daño = dañoBala;
        vida = vidaTotal = segundosVida;
        golpeados.Clear();
        transform.localScale = escalaInicial;
        transform.right = direccion;
        rb.linearVelocity = direccion * velocidad;
        if (render != null) render.color = Color.white;
    }

    void Update()
    {
        vida -= Time.deltaTime;
        if (crecimiento > 0f)
        {
            float t = 1f - vida / vidaTotal;
            transform.localScale = escalaInicial * (1f + crecimiento * t);
            if (render != null) render.color = new Color(1f, 1f, 1f, 1f - t * 0.7f);
        }
        if (vida <= 0f) PoolObjetos.Devolver(gameObject);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!gameObject.activeSelf) return;
        IDañable objetivo = otro.GetComponentInParent<IDañable>();

        if (objetivo != null)
        {
            if (golpeados.Contains(objetivo)) return;
            bool absorbido = objetivo.RecibirDaño(daño, direccion);
            if (!absorbido) return; // por ejemplo: jugador invulnerable rodando
            golpeados.Add(objetivo);
            if (atraviesa && otro.gameObject.layer != Capas.Suelo) return;
            Impactar();
            return;
        }

        if (otro.gameObject.layer == Capas.Suelo && !otro.isTrigger) Impactar();
    }

    void Impactar()
    {
        if (chispasAlImpactar) Efectos.Impacto(transform.position);
        PoolObjetos.Devolver(gameObject);
    }
}
