using UnityEngine;

public enum EstadoJefeCampamento { RafagaAlta, RafagaBaja, Morteros, Descanso }

// Jefe del nivel 1: torreta pesada del campamento. Fija en su lugar, patrón fácil de leer:
//  - Ráfaga ALTA (agáchate)  - Ráfaga BAJA (salta)  - Lluvia de morteros (muévete)
public class JefeCampamento : JefeBase
{
    [Header("Cañones")]
    [SerializeField] Transform cañonAlto;
    [SerializeField] Transform cañonBajo;
    [SerializeField] SpriteRenderer renderCañonAlto;
    [SerializeField] SpriteRenderer renderCañonBajo;
    [SerializeField] Sprite spriteCañon;
    [SerializeField] Sprite spriteCañonFuego;

    [Header("Morteros")]
    [SerializeField] GameObject prefabMortero;
    [SerializeField] Transform bocaMortero;
    [SerializeField] int morterosPorTurno = 3;

    [Header("Ritmo")]
    [SerializeField] int balasPorRafaga = 5;
    [SerializeField] float tiempoEntreBalas = 0.22f;
    [SerializeField] float tiempoDescanso = 1.3f;

    EstadoJefeCampamento estado = EstadoJefeCampamento.Descanso;
    EstadoJefeCampamento siguiente = EstadoJefeCampamento.RafagaAlta;
    float contador;
    int restantes;

    public override void Activar()
    {
        base.Activar();
        estado = EstadoJefeCampamento.Descanso;
        contador = 1f;
    }

    protected override void ActualizarIA()
    {
        bool enojado = salud.Vida < salud.VidaMaxima / 2; // a media vida va más rápido
        float ritmo = enojado ? 1.5f : 1f;
        contador -= Time.deltaTime * ritmo;

        switch (estado)
        {
            case EstadoJefeCampamento.Descanso:
                if (contador <= 0f) Empezar(siguiente);
                break;

            case EstadoJefeCampamento.RafagaAlta:
            case EstadoJefeCampamento.RafagaBaja:
                if (contador <= 0f)
                {
                    bool alta = estado == EstadoJefeCampamento.RafagaAlta;
                    Transform boca = alta ? cañonAlto : cañonBajo;
                    bocaDisparo = boca;
                    DispararBala(Vector2.left, null, 1.3f);
                    AudioManager.Sonar(Sfx.CañonJefe, 0.6f);
                    Fogonazo(alta ? renderCañonAlto : renderCañonBajo);
                    restantes--;
                    contador = tiempoEntreBalas;
                    if (restantes <= 0) Descansar(alta ? EstadoJefeCampamento.RafagaBaja : EstadoJefeCampamento.Morteros);
                }
                break;

            case EstadoJefeCampamento.Morteros:
                if (contador <= 0f && Jugador != null)
                {
                    float x = Jugador.position.x + Random.Range(-1.5f, 1.5f);
                    LanzarMortero(prefabMortero, bocaMortero.position, x, 1.3f);
                    restantes--;
                    contador = 0.45f;
                    if (restantes <= 0) Descansar(EstadoJefeCampamento.RafagaAlta);
                }
                break;
        }
        Animar("Idle");
        RestaurarCañones();
    }

    void Empezar(EstadoJefeCampamento nuevo)
    {
        estado = nuevo;
        contador = 0.2f;
        restantes = nuevo == EstadoJefeCampamento.Morteros ? morterosPorTurno : balasPorRafaga;
    }

    void Descansar(EstadoJefeCampamento despues)
    {
        estado = EstadoJefeCampamento.Descanso;
        siguiente = despues;
        contador = tiempoDescanso;
    }

    float tiempoFogonazo;

    void Fogonazo(SpriteRenderer r)
    {
        if (r == null) return;
        r.sprite = spriteCañonFuego;
        tiempoFogonazo = 0.1f;
        CamaraSeguimiento.Sacudir(0.08f, 0.1f);
    }

    void RestaurarCañones()
    {
        if (tiempoFogonazo <= 0f) return;
        tiempoFogonazo -= Time.deltaTime;
        if (tiempoFogonazo <= 0f)
        {
            if (renderCañonAlto != null) renderCañonAlto.sprite = spriteCañon;
            if (renderCañonBajo != null) renderCañonBajo.sprite = spriteCañon;
        }
    }
}
