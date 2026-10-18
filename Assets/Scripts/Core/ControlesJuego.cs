using UnityEngine;
using UnityEngine.InputSystem;

// Centraliza TODOS los controles (teclado, mouse y gamepad) con el nuevo Input System.
// Los demás scripts solo leen estas propiedades, nunca los dispositivos.
public static class ControlesJuego
{
    static InputAction mover, apuntarArriba, saltar, disparar, granada, rodar, pausa, aceptar;
    static bool iniciado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        iniciado = false;
    }

    static void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;

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

        mover.Enable(); apuntarArriba.Enable(); saltar.Enable(); disparar.Enable(); granada.Enable();
        rodar.Enable(); pausa.Enable(); aceptar.Enable();
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

    public static bool SaltoPresionado { get { Iniciar(); return saltar.WasPressedThisFrame(); } }
    public static bool SaltoMantenido { get { Iniciar(); return saltar.IsPressed(); } }
    public static bool DisparoMantenido { get { Iniciar(); return disparar.IsPressed(); } }
    public static bool GranadaPresionada { get { Iniciar(); return granada.WasPressedThisFrame(); } }
    public static bool RodarPresionado { get { Iniciar(); return rodar.WasPressedThisFrame(); } }
    public static bool PausaPresionada { get { Iniciar(); return pausa.WasPressedThisFrame(); } }
    public static bool AceptarPresionado { get { Iniciar(); return aceptar.WasPressedThisFrame(); } }
}
