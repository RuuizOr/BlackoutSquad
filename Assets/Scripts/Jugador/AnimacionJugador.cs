using UnityEngine;

// Elige qué animación mostrar según el estado del jugador.
// Los sprites de cada animación se cambian en el componente AnimadorSprites (skins).
public class AnimacionJugador : MonoBehaviour
{
    [SerializeField] AnimadorSprites animador;

    JugadorMovimiento movimiento;
    JugadorDisparo disparo;

    void Awake()
    {
        movimiento = GetComponent<JugadorMovimiento>();
        disparo = GetComponent<JugadorDisparo>();
    }

    void LateUpdate()
    {
        animador.Reproducir(ElegirClip());
    }

    string ElegirClip()
    {
        bool disparando = disparo.DisparoReciente;
        switch (movimiento.Estado)
        {
            case EstadoJugador.Muerto:
                return "Muerte";
            case EstadoJugador.Rodando:
                return "Agachado";
            case EstadoJugador.Agachado:
                return disparando ? "AgachadoDisparo" : "Agachado";
            case EstadoJugador.EnAire:
                if (disparo.DireccionApuntado == Apuntado.Arriba || disparo.DireccionApuntado == Apuntado.DiagonalArriba)
                    return "Arriba";
                return disparando ? "SaltoDisparo" : "Salto";
            default:
                if (disparo.DireccionApuntado == Apuntado.Arriba || disparo.DireccionApuntado == Apuntado.DiagonalArriba)
                    return "Arriba";
                bool caminando = Mathf.Abs(movimiento.VelocidadHorizontal) > 0.3f;
                if (caminando) return disparando ? "CorrerDisparo" : "Correr";
                return disparando ? "IdleDisparo" : "Idle";
        }
    }
}
