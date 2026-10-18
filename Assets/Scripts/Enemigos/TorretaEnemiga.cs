using UnityEngine;

public enum EstadoTorreta { Inactiva, Apuntando, Rafaga, Enfriando }

// Torreta fija: cuando ve al jugador dispara ráfagas en línea recta hacia él cada cierto tiempo.
public class TorretaEnemiga : EnemigoBase
{
    [SerializeField] float tiempoApuntar = 0.6f;
    [SerializeField] int balasPorRafaga = 3;
    [SerializeField] float tiempoEntreBalas = 0.14f;
    [SerializeField] float tiempoEnfriamiento = 1.8f;

    EstadoTorreta estado = EstadoTorreta.Inactiva;
    float contador;
    int balasRestantes;
    Vector2 direccionRafaga;

    protected override void ActualizarIA()
    {
        switch (estado)
        {
            case EstadoTorreta.Inactiva:
                Animar("Idle");
                if (JugadorVisible(rangoVision))
                {
                    estado = EstadoTorreta.Apuntando;
                    contador = tiempoApuntar;
                }
                break;

            case EstadoTorreta.Apuntando:
                MirarAlJugador();
                Animar("Idle");
                contador -= Time.deltaTime * Agresividad;
                if (contador <= 0f)
                {
                    direccionRafaga = DireccionAlJugador();
                    balasRestantes = balasPorRafaga;
                    contador = 0f;
                    estado = EstadoTorreta.Rafaga;
                }
                break;

            case EstadoTorreta.Rafaga:
                contador -= Time.deltaTime;
                if (contador <= 0f)
                {
                    DispararBala(direccionRafaga);
                    balasRestantes--;
                    contador = tiempoEntreBalas;
                    if (balasRestantes <= 0)
                    {
                        estado = EstadoTorreta.Enfriando;
                        contador = tiempoEnfriamiento;
                    }
                }
                Animar("Idle");
                break;

            case EstadoTorreta.Enfriando:
                Animar("Idle");
                contador -= Time.deltaTime * Agresividad;
                if (contador <= 0f)
                    estado = JugadorVisible(rangoVision) ? EstadoTorreta.Apuntando : EstadoTorreta.Inactiva;
                if (estado == EstadoTorreta.Apuntando) contador = tiempoApuntar;
                break;
        }
    }
}
