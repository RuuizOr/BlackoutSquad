using System.Collections;
using UnityEngine;

public enum EstadoKruger { Caminar, Disparar, Abanico, Salto, Morteros, Transicion }

// Jefe final: Comandante Kruger, con armadura pesada.
// Fase 1 (100%-50%): camina lento y dispara directo.
// Fase 2 (50%-0%): más rápido, abanico de balas, salto con onda de choque y morteros.
public class JefeKruger : JefeBase
{
    [Header("Movimiento")]
    [SerializeField] float velocidadFase1 = 1.3f;
    [SerializeField] float velocidadFase2 = 2.6f;
    [SerializeField] float distanciaMinima = 4f;
    [SerializeField] float distanciaMaxima = 8f;
    [SerializeField] float fuerzaSalto = 14f;

    [Header("Ataques")]
    [SerializeField] int balasDirectas = 3;
    [SerializeField] int balasAbanico = 7;
    [SerializeField] float anguloAbanico = 70f;
    [SerializeField] GameObject prefabOndaChoque;
    [SerializeField] GameObject prefabMortero;
    [SerializeField] int dañoContacto = 25;
    [SerializeField] Color colorFase2 = new Color(1f, 0.6f, 0.55f);

    EstadoKruger estado = EstadoKruger.Caminar;
    int fase = 1;
    float contador;
    int restantes;
    bool enElAire;
    float tiempoEnAire;

    public int Fase => fase;

    public override void Activar()
    {
        base.Activar();
        salud.AlRecibirDaño += RevisarFase;
        Cambiar(EstadoKruger.Caminar, 1.5f);
    }

    void RevisarFase()
    {
        if (fase == 1 && salud.Vida <= salud.VidaMaxima / 2) StartCoroutine(TransicionFase2());
    }

    IEnumerator TransicionFase2()
    {
        fase = 2;
        estado = EstadoKruger.Transicion;
        salud.Invulnerable = true;
        Detener();
        AudioManager.Sonar(Sfx.CambioFase);
        CamaraSeguimiento.Sacudir(0.4f, 1f);
        if (HUD.Instancia != null) HUD.Instancia.MostrarAviso("¡KRUGER ESTÁ FURIOSO!", 1.6f);
        SpriteRenderer r = animador.Render;
        for (int i = 0; i < 8; i++)
        {
            r.color = i % 2 == 0 ? Color.white * 2f : colorFase2;
            yield return new WaitForSeconds(0.12f);
        }
        salud.CambiarColorBase(colorFase2);
        Efectos.Explosion(transform.position + Vector3.up * 2f, true);
        salud.Invulnerable = false;
        Cambiar(EstadoKruger.Abanico, 0.4f);
    }

    protected override void ActualizarIA()
    {
        if (estado == EstadoKruger.Transicion) return;
        contador -= Time.deltaTime;
        DañoPorContacto((Vector2)transform.position + Vector2.up * 1.8f, new Vector2(1.6f, 3.4f), dañoContacto);

        switch (estado)
        {
            case EstadoKruger.Caminar:
                MirarAlJugador();
                float dx = Jugador.position.x - transform.position.x;
                float vel = fase == 1 ? velocidadFase1 : velocidadFase2;
                float vx = 0f;
                if (Mathf.Abs(dx) > distanciaMaxima) vx = Mathf.Sign(dx) * vel;
                else if (Mathf.Abs(dx) < distanciaMinima) vx = -Mathf.Sign(dx) * vel * 0.6f;
                Mover(vx);
                Animar(vx != 0f ? "Caminar" : "Idle");
                if (contador <= 0f) ElegirAtaque();
                break;

            case EstadoKruger.Disparar:
                Mover(0f);
                MirarAlJugador();
                Animar("Idle");
                if (contador <= 0f)
                {
                    DispararBala(DireccionAlJugador(), null, 1f);
                    restantes--;
                    contador = fase == 1 ? 0.35f : 0.18f;
                    if (restantes <= 0) Cambiar(EstadoKruger.Caminar, fase == 1 ? 1.8f : 1.1f);
                }
                break;

            case EstadoKruger.Abanico:
                Mover(0f);
                MirarAlJugador();
                Animar("Idle");
                if (contador <= 0f)
                {
                    DispararAbanico();
                    restantes--;
                    contador = 0.7f;
                    if (restantes <= 0) Cambiar(EstadoKruger.Caminar, 1f);
                }
                break;

            case EstadoKruger.Salto:
                ActualizarSalto();
                break;

            case EstadoKruger.Morteros:
                Mover(0f);
                Animar("Idle");
                if (contador <= 0f)
                {
                    float x = Jugador.position.x + Random.Range(-2f, 2f);
                    LanzarMortero(prefabMortero, transform.position + Vector3.up * 3.5f, x, 1.1f);
                    restantes--;
                    contador = 0.35f;
                    if (restantes <= 0) Cambiar(EstadoKruger.Caminar, 1f);
                }
                break;
        }

        // No sale de la arena
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, arenaMinX + 1.5f, arenaMaxX - 1.5f);
        transform.position = p;
    }

    void ElegirAtaque()
    {
        if (fase == 1)
        {
            restantes = balasDirectas;
            Cambiar(EstadoKruger.Disparar, 0.2f);
            return;
        }
        int r = Random.Range(0, 4);
        if (r == 0) { restantes = 2; Cambiar(EstadoKruger.Abanico, 0.3f); }
        else if (r == 1) { EmpezarSalto(); }
        else if (r == 2) { restantes = 4; Cambiar(EstadoKruger.Morteros, 0.3f); }
        else { restantes = balasDirectas + 2; Cambiar(EstadoKruger.Disparar, 0.2f); }
    }

    void Cambiar(EstadoKruger nuevo, float espera)
    {
        estado = nuevo;
        contador = espera;
        if (nuevo == EstadoKruger.Abanico && restantes <= 0) restantes = 2;
    }

    void DispararAbanico()
    {
        Vector2 baseDir = DireccionAlJugador();
        float anguloBase = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        for (int i = 0; i < balasAbanico; i++)
        {
            float a = anguloBase + Mathf.Lerp(-anguloAbanico / 2f, anguloAbanico / 2f, i / (float)(balasAbanico - 1));
            DispararBala(new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)), null, 0.85f);
        }
        AudioManager.Sonar(Sfx.CañonJefe, 0.7f);
    }

    void EmpezarSalto()
    {
        estado = EstadoKruger.Salto;
        MirarAlJugador();
        float dx = Mathf.Clamp(Jugador.position.x - transform.position.x, -6f, 6f);
        rb.linearVelocity = new Vector2(dx / 1.2f, fuerzaSalto);
        enElAire = true;
        tiempoEnAire = 0f;
        AudioManager.Sonar(Sfx.Salto);
    }

    void ActualizarSalto()
    {
        Animar("Caminar");
        tiempoEnAire += Time.deltaTime;
        bool tocaSuelo = Physics2D.OverlapBox((Vector2)transform.position, new Vector2(1.2f, 0.15f), 0f, Capas.MascaraSuelo) != null;
        if (enElAire && tiempoEnAire > 0.25f && tocaSuelo && rb.linearVelocity.y <= 0.1f)
        {
            enElAire = false;
            Mover(0f);
            // Aterrizaje: onda de choque hacia ambos lados (hay que saltarla)
            CamaraSeguimiento.Sacudir(0.45f, 0.4f);
            Efectos.Explosion(transform.position + Vector3.up * 0.3f, false);
            CrearOnda(Vector2.left);
            CrearOnda(Vector2.right);
            Cambiar(EstadoKruger.Caminar, 1.2f);
        }
    }

    void CrearOnda(Vector2 dir)
    {
        if (prefabOndaChoque == null) return;
        GameObject o = PoolObjetos.Sacar(prefabOndaChoque, transform.position + new Vector3(dir.x * 1.2f, 0.45f, 0f), Quaternion.identity);
        o.GetComponent<Bala>().Disparar(dir, 7.5f, dañoBala, 2.5f);
    }

    void Mover(float vx)
    {
        if (rb != null) rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }
}
