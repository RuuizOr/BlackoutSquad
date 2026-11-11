using System.Collections;
using UnityEngine;

// Vida del jugador: daño con flash, retroceso, sonido y sacudida; invulnerabilidad con parpadeo.
public class VidaJugador : MonoBehaviour, IDañable
{
    [SerializeField] int vidaMaxima = 100;
    [SerializeField] float invulnerabilidadTrasDaño = 0.8f;
    [SerializeField] float invulnerabilidadAlReaparecer = 2f;
    [SerializeField] Vector2 fuerzaRetroceso = new Vector2(4f, 4f);
    [SerializeField] SpriteRenderer render;
    [SerializeField] Color colorDaño = new Color(1f, 0.35f, 0.35f);

    JugadorMovimiento movimiento;
    float tiempoInvulnerable;

    public int Vida { get; private set; }
    public int VidaMaxima => vidaMaxima;
    public bool Invulnerable => tiempoInvulnerable > 0f || movimiento.InvulnerablePorRodar;

    void Awake()
    {
        movimiento = GetComponent<JugadorMovimiento>();
        Vida = vidaMaxima;
    }

    void Update()
    {
        if (tiempoInvulnerable > 0f)
        {
            tiempoInvulnerable -= Time.deltaTime;
            // parpadeo
            render.enabled = tiempoInvulnerable <= 0f || Mathf.FloorToInt(tiempoInvulnerable * 15f) % 2 == 0;
        }
        else if (!render.enabled) render.enabled = true;
    }

    public bool RecibirDaño(int cantidad, Vector2 direccion)
    {
        if (movimiento.EstaMuerto || Invulnerable) return false; // la bala pasa de largo
        if (ControladorNivel.Instancia != null && !ControladorNivel.Instancia.JuegoActivo) return false;

        Vida = Mathf.Max(0, Vida - cantidad);
        AudioManager.Sonar(Sfx.DañoJugador);
        CamaraSeguimiento.Sacudir(0.2f, 0.2f);
        StartCoroutine(Flash());
        GestorVisual.DestelloDaño();
        Vibracion.Vibrar(Vida <= 0 ? 260 : 70);

        if (Vida <= 0)
        {
            Morir();
            return true;
        }

        tiempoInvulnerable = invulnerabilidadTrasDaño;
        float lado = direccion.x != 0f ? Mathf.Sign(direccion.x) : (movimiento.MirandoDerecha ? -1f : 1f);
        movimiento.Empujar(new Vector2(fuerzaRetroceso.x * lado, fuerzaRetroceso.y));
        return true;
    }

    IEnumerator Flash()
    {
        render.color = colorDaño;
        yield return new WaitForSeconds(0.1f);
        render.color = Color.white;
    }

    public void Curar(int cantidad)
    {
        Vida = Mathf.Min(vidaMaxima, Vida + cantidad);
    }

    // Foso o agua: pierde la vida al instante
    public void MorirInstantaneo()
    {
        if (movimiento.EstaMuerto) return;
        Vida = 0;
        AudioManager.Sonar(Sfx.CaerFoso);
        GestorVisual.DestelloDaño();
        Vibracion.Vibrar(260);
        Morir();
    }

    void Morir()
    {
        movimiento.Morir();
        GetComponent<JugadorDisparo>().PerderArmaEspecial();
        render.enabled = true;
        if (ControladorNivel.Instancia != null) ControladorNivel.Instancia.JugadorMurio();
    }

    public void Reaparecer()
    {
        Vida = vidaMaxima;
        tiempoInvulnerable = invulnerabilidadAlReaparecer;
        render.color = Color.white;
        GetComponent<JugadorDisparo>().RecargarGranadas();
    }
}
