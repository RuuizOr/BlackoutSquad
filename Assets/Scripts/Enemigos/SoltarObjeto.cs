using System;
using UnityEngine;

[Serializable]
public class PosibleDrop
{
    public GameObject prefab;
    [Range(0f, 1f)] public float probabilidad = 0.1f;
}

// Al morir, el enemigo puede soltar un objeto (se revisa la lista en orden y suelta el primero que salga).
public class SoltarObjeto : MonoBehaviour
{
    [SerializeField] PosibleDrop[] posibles;

    public void Soltar()
    {
        if (posibles == null) return;
        foreach (PosibleDrop p in posibles)
        {
            if (p.prefab == null) continue;
            float prob = p.probabilidad;
            // En niveles más difíciles salen menos botiquines
            ItemRecogible item = p.prefab.GetComponent<ItemRecogible>();
            if (item != null && item.Tipo == TipoItem.Botiquin && ControladorNivel.Instancia != null)
                prob *= ControladorNivel.Instancia.MultiplicadorBotiquines;

            if (UnityEngine.Random.value < prob)
            {
                Instantiate(p.prefab, transform.position + Vector3.up * 0.4f, Quaternion.identity);
                return;
            }
        }
    }
}
