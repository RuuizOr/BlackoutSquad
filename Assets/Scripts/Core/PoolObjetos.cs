using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Pool simple de objetos: reutiliza balas, efectos y popups en lugar de crear/destruir.
public static class PoolObjetos
{
    static readonly Dictionary<GameObject, Stack<GameObject>> libres = new Dictionary<GameObject, Stack<GameObject>>();
    static readonly Dictionary<GameObject, GameObject> origen = new Dictionary<GameObject, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Iniciar()
    {
        libres.Clear();
        origen.Clear();
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    // Los objetos de la escena anterior ya no existen: vaciamos el pool.
    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        libres.Clear();
        origen.Clear();
    }

    public static GameObject Sacar(GameObject prefab, Vector3 posicion, Quaternion rotacion)
    {
        if (prefab == null) return null;
        if (libres.TryGetValue(prefab, out Stack<GameObject> pila))
        {
            while (pila.Count > 0)
            {
                GameObject obj = pila.Pop();
                if (obj == null) continue;
                obj.transform.SetPositionAndRotation(posicion, rotacion);
                obj.SetActive(true);
                return obj;
            }
        }
        GameObject nuevo = Object.Instantiate(prefab, posicion, rotacion);
        origen[nuevo] = prefab;
        return nuevo;
    }

    public static void Devolver(GameObject obj)
    {
        if (obj == null || !obj.activeSelf) return;
        if (!origen.TryGetValue(obj, out GameObject prefab))
        {
            Object.Destroy(obj); // no venía del pool
            return;
        }
        obj.SetActive(false);
        if (!libres.TryGetValue(prefab, out Stack<GameObject> pila))
        {
            pila = new Stack<GameObject>();
            libres[prefab] = pila;
        }
        pila.Push(obj);
    }
}
