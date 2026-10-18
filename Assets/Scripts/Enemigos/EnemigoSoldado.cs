using UnityEngine;

public enum EstadoSoldado { Patrulla, Persecucion, Disparo, Regreso }

// Soldado terrestre (raso, con escudo o élite: mismo script, distintos valores en el prefab).
// Patrulla → detecta al jugador → persigue/dispara → lo pierde de vista → regresa a su puesto.
public class EnemigoSoldado : EnemigoBase
{
    [Header("Movimiento")]
    [SerializeField] float velocidadPatrulla = 1.2f;
    [SerializeField] float velocidadPersecucion = 2.4f;
    [SerializeField] float radioPatrulla = 2.5f;
    [SerializeField] float pausaEnBorde = 1f;

    [Header("Combate")]
    [SerializeField] float distanciaDisparo = 7f;
    [SerializeField] float distanciaMinima = 3f;        // no se acerca más que esto
    [SerializeField] float tiempoEntreRafagas = 1.6f;
    [SerializeField] int balasPorRafaga = 1;
    [SerializeField] float tiempoEntreBalas = 0.15f;
    [SerializeField] bool apuntaDirecto;                 // élite: apunta al jugador; los demás disparan recto
    [SerializeField] bool avanzaMientrasDispara;         // soldado con escudo: avanza cubierto
    [SerializeField] float tiempoParaOlvidar = 2.5f;

    EstadoSoldado estado = EstadoSoldado.Patrulla;
    float origenX;
    float destinoPatrulla;
    float esperaBorde, contadorRafaga, contadorBala, tiempoSinVer;
    int balasRestantes;

    protected override void Awake()
    {
        base.Awake();
        origenX = transform.position.x;
        destinoPatrulla = origenX + radioPatrulla;
        contadorRafaga = Random.Range(0.4f, 1.2f);
    }

    protected override void ActualizarIA()
    {
        switch (estado)
        {
            case EstadoSoldado.Patrulla: Patrullar(); break;
            case EstadoSoldado.Persecucion: Perseguir(); break;
            case EstadoSoldado.Disparo: DispararAlJugador(); break;
            case EstadoSoldado.Regreso: Regresar(); break;
        }
    }

    void Patrullar()
    {
        if (JugadorVisible(rangoVision)) { estado = EstadoSoldado.Persecucion; return; }

        if (esperaBorde > 0f)
        {
            esperaBorde -= Time.deltaTime;
            Mover(0f);
            Animar("Idle");
            return;
        }
        if (Mathf.Abs(transform.position.x - destinoPatrulla) < 0.1f || !HaySueloAdelante())
        {
            destinoPatrulla = destinoPatrulla > origenX ? origenX - radioPatrulla : origenX + radioPatrulla;
            esperaBorde = pausaEnBorde;
            return;
        }
        MirarHacia(destinoPatrulla);
        Mover(velocidadPatrulla * Lado);
        Animar("Caminar");
    }

    void Perseguir()
    {
        if (!ActualizarMemoria()) return;
        MirarAlJugador();
        float dist = Mathf.Abs(Jugador.position.x - transform.position.x);
        if (dist <= distanciaDisparo)
        {
            estado = EstadoSoldado.Disparo;
            return;
        }
        Mover(HaySueloAdelante() ? velocidadPersecucion * Lado : 0f);
        Animar("Caminar");
    }

    void DispararAlJugador()
    {
        if (!ActualizarMemoria()) return;
        MirarAlJugador();
        float dist = Mathf.Abs(Jugador.position.x - transform.position.x);

        // Se aleja demasiado: volver a perseguir
        if (dist > distanciaDisparo * 1.3f) { estado = EstadoSoldado.Persecucion; return; }

        bool avanzar = avanzaMientrasDispara && dist > distanciaMinima && HaySueloAdelante();
        Mover(avanzar ? velocidadPatrulla * Lado : 0f);

        // Ráfagas
        if (balasRestantes > 0)
        {
            contadorBala -= Time.deltaTime;
            if (contadorBala <= 0f)
            {
                Vector2 dir = apuntaDirecto ? DireccionAlJugador() : new Vector2(Lado, 0f);
                DispararBala(dir);
                balasRestantes--;
                contadorBala = tiempoEntreBalas;
            }
        }
        else
        {
            contadorRafaga -= Time.deltaTime * Agresividad;
            if (contadorRafaga <= 0f)
            {
                balasRestantes = balasPorRafaga;
                contadorBala = 0f;
                contadorRafaga = tiempoEntreRafagas * Random.Range(0.85f, 1.15f);
            }
        }
        Animar(avanzar ? "Caminar" : "Idle");
    }

    void Regresar()
    {
        if (JugadorVisible(rangoVision)) { estado = EstadoSoldado.Persecucion; return; }
        if (Mathf.Abs(transform.position.x - origenX) < 0.15f || !HaySueloAdelante())
        {
            estado = EstadoSoldado.Patrulla;
            destinoPatrulla = origenX + radioPatrulla;
            return;
        }
        MirarHacia(origenX);
        Mover(velocidadPatrulla * Lado);
        Animar("Caminar");
    }

    // Si no ve al jugador por un rato, lo olvida y regresa a patrullar
    bool ActualizarMemoria()
    {
        if (Jugador == null) { estado = EstadoSoldado.Regreso; return false; }
        if (JugadorVisible(rangoVision * 1.2f)) tiempoSinVer = 0f;
        else tiempoSinVer += Time.deltaTime;
        if (tiempoSinVer > tiempoParaOlvidar)
        {
            estado = EstadoSoldado.Regreso;
            tiempoSinVer = 0f;
            return false;
        }
        return true;
    }

    bool HaySueloAdelante()
    {
        Vector2 origen = (Vector2)transform.position + new Vector2(Lado * 0.6f, 0.3f);
        bool suelo = Physics2D.Raycast(origen, Vector2.down, 1.2f, Capas.MascaraSuelo).collider != null;
        bool pared = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.5f, new Vector2(Lado, 0f), 0.6f, Capas.MascaraSuelo).collider != null;
        return suelo && !pared;
    }

    void Mover(float vx)
    {
        if (rb != null) rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, rangoVision);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, distanciaDisparo);
    }
}
