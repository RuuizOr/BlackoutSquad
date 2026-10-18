using UnityEngine;

public enum EstadoVehiculo { Avanzar, Disparar, PrepararEmbestida, Embestida, Aturdido }

// Mini-jefe tanque (nivel 2): persigue dentro de la arena, dispara obuses y embiste.
// Su frente está blindado (ver SaludEnemigo.proteccionFrontal): hay que darle por detrás,
// desde arriba o con granadas.
public class VehiculoMiniJefe : JefeBase
{
    [Header("Tanque")]
    [SerializeField] float velocidadAvance = 1.6f;
    [SerializeField] float velocidadEmbestida = 9.5f;
    [SerializeField] float distanciaPreferida = 6f;
    [SerializeField] float tiempoAvance = 2.2f;
    [SerializeField] int disparosPorTurno = 2;
    [SerializeField] float tiempoEntreDisparos = 0.8f;
    [SerializeField] float tiempoPreparacion = 0.8f;
    [SerializeField] float tiempoAturdido = 1.6f;
    [SerializeField] int dañoContacto = 30;
    [SerializeField] Vector2 cajaContacto = new Vector2(2.6f, 1.6f);

    EstadoVehiculo estado = EstadoVehiculo.Avanzar;
    float contador;
    int disparosRestantes;
    float direccionEmbestida;
    int turnos;

    public override void Activar()
    {
        base.Activar();
        estado = EstadoVehiculo.Avanzar;
        contador = tiempoAvance;
    }

    protected override void ActualizarIA()
    {
        contador -= Time.deltaTime;
        DañoPorContacto((Vector2)transform.position + Vector2.up * 0.8f, cajaContacto, dañoContacto);

        switch (estado)
        {
            case EstadoVehiculo.Avanzar:
                MirarAlJugador();
                float dx = Jugador.position.x - transform.position.x;
                float vx = Mathf.Abs(dx) > distanciaPreferida ? Mathf.Sign(dx) * velocidadAvance : 0f;
                Mover(vx);
                animador.Reproducir(vx != 0f ? "Mover" : "Idle");
                if (contador <= 0f)
                {
                    turnos++;
                    if (turnos % 2 == 0) { estado = EstadoVehiculo.PrepararEmbestida; contador = tiempoPreparacion; AudioManager.Sonar(Sfx.AlarmaJefe, 0.5f); }
                    else { estado = EstadoVehiculo.Disparar; disparosRestantes = disparosPorTurno; contador = 0.3f; }
                }
                break;

            case EstadoVehiculo.Disparar:
                Mover(0f);
                MirarAlJugador();
                animador.Reproducir("Idle");
                if (contador <= 0f)
                {
                    DispararBala(new Vector2(Lado, 0f), null, 1f);
                    AudioManager.Sonar(Sfx.CañonJefe);
                    CamaraSeguimiento.Sacudir(0.12f, 0.15f);
                    disparosRestantes--;
                    contador = tiempoEntreDisparos / Agresividad;
                    if (disparosRestantes <= 0) { estado = EstadoVehiculo.Avanzar; contador = tiempoAvance; }
                }
                break;

            case EstadoVehiculo.PrepararEmbestida:
                // Tiembla y parpadea como aviso
                Mover(0f);
                MirarAlJugador();
                transform.position += new Vector3(Random.Range(-0.03f, 0.03f), 0f, 0f);
                animador.Reproducir("Mover");
                if (contador <= 0f)
                {
                    direccionEmbestida = Lado;
                    estado = EstadoVehiculo.Embestida;
                    CamaraSeguimiento.Sacudir(0.15f, 0.3f);
                }
                break;

            case EstadoVehiculo.Embestida:
                Mover(direccionEmbestida * velocidadEmbestida);
                animador.Reproducir("Mover");
                bool llegoAlBorde = (direccionEmbestida > 0f && transform.position.x >= arenaMaxX - 2f) ||
                                    (direccionEmbestida < 0f && transform.position.x <= arenaMinX + 2f);
                if (llegoAlBorde)
                {
                    Mover(0f);
                    CamaraSeguimiento.Sacudir(0.3f, 0.3f);
                    AudioManager.Sonar(Sfx.ExplosionChica);
                    estado = EstadoVehiculo.Aturdido;
                    contador = tiempoAturdido;
                }
                break;

            case EstadoVehiculo.Aturdido:
                // Queda de espaldas al jugador: ¡momento de dispararle atrás!
                Mover(0f);
                animador.Reproducir("Idle");
                if (contador <= 0f) { estado = EstadoVehiculo.Avanzar; contador = tiempoAvance; }
                break;
        }

        // Nunca sale de la arena
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, arenaMinX + 1.5f, arenaMaxX - 1.5f);
        transform.position = p;
    }

    void Mover(float vx)
    {
        if (rb != null) rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, cajaContacto);
    }
}
