using System.Collections.Generic;
using UnityEngine;

// Aplica daño a todo lo dañable dentro de un círculo (granadas, misiles, explosiones).
public static class DañoArea
{
    static readonly List<IDañable> yaDañados = new List<IDañable>();

    public static void Aplicar(Vector2 centro, float radio, int daño, int mascara)
    {
        yaDañados.Clear();
        Collider2D[] golpeados = Physics2D.OverlapCircleAll(centro, radio, mascara);
        foreach (Collider2D c in golpeados)
        {
            IDañable d = c.GetComponentInParent<IDañable>();
            if (d == null || yaDañados.Contains(d)) continue;
            yaDañados.Add(d);
            d.RecibirDaño(daño, Vector2.zero); // dirección cero: ignora escudos
        }
    }
}
