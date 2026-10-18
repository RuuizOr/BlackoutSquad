using UnityEngine;

// Números de las capas (Layers) del juego. El constructor del editor las crea con estos índices.
public static class Capas
{
    public const int Jugador = 6;
    public const int Enemigo = 7;
    public const int BalaJugador = 8;
    public const int BalaEnemigo = 9;
    public const int Suelo = 10;
    public const int Item = 11;
    public const int Disparador = 12;

    public static readonly string[] Nombres =
    {
        "Jugador", "Enemigo", "BalaJugador", "BalaEnemigo", "Suelo", "Item", "Disparador"
    };

    public static int MascaraSuelo => 1 << Suelo;
    public static int MascaraJugador => 1 << Jugador;
    public static int MascaraEnemigo => 1 << Enemigo;
}
