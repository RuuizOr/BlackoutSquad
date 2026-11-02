using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

public enum TipoControl { Teclado, Xbox, PlayStation, Generico }

// Centraliza TODOS los controles (teclado, mouse y gamepad) con el nuevo Input System.
// Los demás scripts solo leen estas propiedades, nunca los dispositivos.
// Xbox (One, Series S/X) entra como XInput y PlayStation (4 y 5) como DualShock/DualSense;
// las acciones usan <Gamepad>, así que los mismos botones físicos sirven para todos.
public static class ControlesJuego
{
    static InputAction mover, apuntarArriba, saltar, disparar, granada, rodar, pausa, aceptar, cancelar;
    static bool iniciado;
    static int frameSinAcciones = -1;
    static TipoControl tipoActual;

    // Último mando que usó el jugador (null si no hay ninguno)
    public static Gamepad MandoActivo { get; private set; }

    // Último tipo de control usado: decide qué botones se muestran en pantalla
    public static TipoControl TipoActual { get { Iniciar(); return tipoActual; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        iniciado = false;
        frameSinAcciones = -1;
        tipoActual = TipoControl.Teclado;
        MandoActivo = null;
    }

    public static TipoControl Clasificar(InputDevice dispositivo)
    {
        if (dispositivo is XInputController) return TipoControl.Xbox;
        if (dispositivo is DualShockGamepad) return TipoControl.PlayStation; // incluye DualSense
        return dispositivo is Gamepad ? TipoControl.Generico : TipoControl.Teclado;
    }

    public static void UsarMando(Gamepad mando)
    {
        MandoActivo = mando;
        tipoActual = mando != null ? Clasificar(mando) : TipoControl.Teclado;
    }

    // Cualquier acción ejecutada indica con qué está jugando (incluye mover el mouse en la UI)
    static void AlCambiarAccion(object objeto, InputActionChange cambio)
    {
        if (cambio != InputActionChange.ActionPerformed) return;
        InputControl control = ((InputAction)objeto).activeControl;
        if (control == null) return;
        if (control.device is Gamepad mando) UsarMando(mando);
        else if (control.device is Keyboard || control.device is Mouse) tipoActual = TipoControl.Teclado;
    }

    // Lo que se presionó en este frame fue para un menú: no debe convertirse en salto, granada o giro
    public static void IgnorarAccionesEsteFrame()
    {
        frameSinAcciones = Time.frameCount;
    }

    static bool AccionesPermitidas => Time.frameCount != frameSinAcciones;

    static void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;

        UsarMando(Gamepad.current);
        InputSystem.onActionChange -= AlCambiarAccion;
        InputSystem.onActionChange += AlCambiarAccion;

        // En teclado W es saltar, así que "arriba" NO va en el movimiento: se apunta con la flecha arriba
        mover = new InputAction("Mover", InputActionType.Value);
        mover.AddCompositeBinding("2DVector")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        mover.AddCompositeBinding("2DVector")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        mover.AddBinding("<Gamepad>/leftStick");
        mover.AddBinding("<Gamepad>/dpad");

        apuntarArriba = new InputAction("ApuntarArriba", InputActionType.Button);
        apuntarArriba.AddBinding("<Keyboard>/upArrow");

        saltar = new InputAction("Saltar", InputActionType.Button);
        saltar.AddBinding("<Keyboard>/w");
        saltar.AddBinding("<Gamepad>/buttonSouth");

        disparar = new InputAction("Disparar", InputActionType.Button);
        disparar.AddBinding("<Keyboard>/space");
        disparar.AddBinding("<Gamepad>/rightTrigger");

        granada = new InputAction("Granada", InputActionType.Button);
        granada.AddBinding("<Keyboard>/e");
        granada.AddBinding("<Gamepad>/buttonEast");

        rodar = new InputAction("Rodar", InputActionType.Button);
        rodar.AddBinding("<Keyboard>/leftShift");
        rodar.AddBinding("<Keyboard>/rightShift");
        rodar.AddBinding("<Gamepad>/buttonWest");

        pausa = new InputAction("Pausa", InputActionType.Button);
        pausa.AddBinding("<Keyboard>/escape");
        pausa.AddBinding("<Gamepad>/start");

        aceptar = new InputAction("Aceptar", InputActionType.Button);
        aceptar.AddBinding("<Keyboard>/enter");
        aceptar.AddBinding("<Keyboard>/space");
        aceptar.AddBinding("<Mouse>/leftButton");
        aceptar.AddBinding("<Gamepad>/buttonSouth");
        aceptar.AddBinding("<Gamepad>/start");

        // Solo mando: en teclado ESC ya hace esta función con la acción de pausa
        cancelar = new InputAction("Cancelar", InputActionType.Button);
        cancelar.AddBinding("<Gamepad>/buttonEast");

        mover.Enable(); apuntarArriba.Enable(); saltar.Enable(); disparar.Enable(); granada.Enable();
        rodar.Enable(); pausa.Enable(); aceptar.Enable(); cancelar.Enable();
    }

    // Dirección analógica cruda (-1..1)
    public static Vector2 Direccion { get { Iniciar(); return mover.ReadValue<Vector2>(); } }

    // Ejes "digitales": -1, 0 o 1 (con zona muerta para el stick)
    public static int Horizontal
    {
        get { float x = Direccion.x; return x > 0.4f ? 1 : (x < -0.4f ? -1 : 0); }
    }

    public static int Vertical
    {
        get
        {
            Iniciar();
            if (apuntarArriba.IsPressed()) return 1; // flecha arriba
            float y = Direccion.y; // stick del gamepad o S / flecha abajo
            return y > 0.5f ? 1 : (y < -0.5f ? -1 : 0);
        }
    }

    public static bool SaltoPresionado { get { Iniciar(); return AccionesPermitidas && saltar.WasPressedThisFrame(); } }
    public static bool SaltoMantenido { get { Iniciar(); return saltar.IsPressed(); } }
    public static bool DisparoMantenido { get { Iniciar(); return disparar.IsPressed(); } }
    public static bool GranadaPresionada { get { Iniciar(); return AccionesPermitidas && granada.WasPressedThisFrame(); } }
    public static bool RodarPresionado { get { Iniciar(); return AccionesPermitidas && rodar.WasPressedThisFrame(); } }
    public static bool PausaPresionada { get { Iniciar(); return pausa.WasPressedThisFrame(); } }
    public static bool AceptarPresionado { get { Iniciar(); return aceptar.WasPressedThisFrame(); } }
    public static bool CancelarPresionado { get { Iniciar(); return cancelar.WasPressedThisFrame(); } }

    // ---------- Textos de ayuda según el control en uso ----------
    public static string TextoAyudaControles
    {
        get
        {
            switch (TipoActual)
            {
                case TipoControl.Xbox:
                    return "STICK / CRUCETA MOVER Y APUNTAR · A SALTAR · RT DISPARAR · B GRANADA · X RODAR · MENU PAUSA";
                case TipoControl.PlayStation:
                    return "STICK / CRUCETA MOVER Y APUNTAR · CRUZ SALTAR · R2 DISPARAR · CIRCULO GRANADA · CUADRADO RODAR · OPTIONS PAUSA";
                case TipoControl.Generico:
                    return "STICK / CRUCETA MOVER Y APUNTAR · BOTON ABAJO SALTAR · GATILLO DERECHO DISPARAR · BOTON DERECHO GRANADA · BOTON IZQUIERDO RODAR · START PAUSA";
                default:
                    return "A/D MOVER · W SALTAR · ESPACIO DISPARAR · ↑ APUNTAR ARRIBA · S AGACHARSE · E GRANADA · SHIFT RODAR · ESC PAUSA";
            }
        }
    }

    public static string TextoBotonAceptar
    {
        get
        {
            switch (TipoActual)
            {
                case TipoControl.Xbox: return "A";
                case TipoControl.PlayStation: return "CRUZ";
                case TipoControl.Generico: return "BOTON ABAJO";
                default: return "ENTER / ESPACIO";
            }
        }
    }
}
