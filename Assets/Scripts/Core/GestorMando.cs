using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Conexión y desconexión de mandos durante el juego. También oculta el cursor mientras se usa
// un mando y recupera la selección de los menús si se perdió (por ejemplo, tras un clic en el fondo).
// Lo crea Arranque y persiste entre escenas.
public class GestorMando : MonoBehaviour
{
    void OnEnable()
    {
        InputSystem.onDeviceChange += AlCambiarDispositivo;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= AlCambiarDispositivo;
    }

    void AlCambiarDispositivo(InputDevice dispositivo, InputDeviceChange cambio)
    {
        if (!(dispositivo is Gamepad mando)) return;
        switch (cambio)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                ControlesJuego.UsarMando(mando);
                if (HUD.Instancia != null) HUD.Instancia.MostrarAviso(NombreMando(mando) + " CONECTADO", 1.5f);
                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                if (mando != ControlesJuego.MandoActivo) break;
                bool estabaEnUso = ControlesJuego.TipoActual != TipoControl.Teclado;
                ControlesJuego.UsarMando(OtroMando(mando));
                // Si se jugaba con él y no queda otro, se pausa para que el jugador no muera mientras tanto
                if (estabaEnUso && ControlesJuego.MandoActivo == null && MenuPausa.Instancia != null) MenuPausa.Instancia.PausarSinMando();
                break;
        }
    }

    static Gamepad OtroMando(Gamepad quitado)
    {
        foreach (Gamepad g in Gamepad.all)
            if (g != quitado) return g;
        return null;
    }

    static string NombreMando(Gamepad mando)
    {
        switch (ControlesJuego.Clasificar(mando))
        {
            case TipoControl.Xbox: return "MANDO XBOX";
            case TipoControl.PlayStation: return "MANDO PLAYSTATION";
            default: return "MANDO";
        }
    }

    void Update()
    {
        Cursor.visible = ControlesJuego.TipoActual == TipoControl.Teclado;
        RecuperarSeleccion();
    }

    void RecuperarSeleccion()
    {
        EventSystem es = EventSystem.current;
        Gamepad g = Gamepad.current;
        if (es == null || g == null || Selectable.allSelectableCount == 0) return; // en pleno juego no hay menús

        GameObject actual = es.currentSelectedGameObject;
        if (actual != null && actual.activeInHierarchy) return;

        bool tocoMando = g.leftStick.ReadValue().sqrMagnitude > 0.25f || g.dpad.ReadValue().sqrMagnitude > 0.25f || g.buttonSouth.wasPressedThisFrame;
        if (!tocoMando) return;

        // Selecciona el botón de más arriba de los que estén visibles
        Selectable mejor = null;
        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (!s.IsInteractable() || !s.gameObject.activeInHierarchy) continue;
            if (mejor == null || s.transform.position.y > mejor.transform.position.y) mejor = s;
        }
        if (mejor != null) es.SetSelectedGameObject(mejor.gameObject);
    }
}
