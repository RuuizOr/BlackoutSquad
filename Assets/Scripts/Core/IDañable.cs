using UnityEngine;

// Todo lo que puede recibir daño (jugador, enemigos, cofres, plataformas) implementa esta interfaz.
public interface IDañable
{
    // direccion: hacia dónde viajaba el golpe (Vector2.zero = explosión).
    // Devuelve true si el golpe fue "absorbido" (la bala se destruye).
    bool RecibirDaño(int cantidad, Vector2 direccion);
}
