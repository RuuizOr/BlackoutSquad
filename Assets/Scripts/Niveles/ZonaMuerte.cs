using UnityEngine;

// Fosos, agua y vacío: el jugador pierde una vida al caer; los enemigos mueren.
public class ZonaMuerte : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D otro)
    {
        VidaJugador jugador = otro.GetComponentInParent<VidaJugador>();
        if (jugador != null)
        {
            jugador.MorirInstantaneo();
            return;
        }
        SaludEnemigo enemigo = otro.GetComponentInParent<SaludEnemigo>();
        if (enemigo != null && !enemigo.EsJefe) enemigo.Matar();
    }
}
