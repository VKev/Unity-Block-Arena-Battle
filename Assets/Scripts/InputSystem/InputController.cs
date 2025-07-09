using System;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class InputController : MonoBehaviour
{
    public static InputSystem InputSystem;

    public static InputActionBuilder
        WalkAction,
        RunAction,
        JumpAction,
        ZoomAction,
        EscAction,
        AltAction,
        InteractAction,
        MouseAction,
        OpenInventoryAction,
        SwitchWeaponAction,
        FireAction;

    private void Awake()
    {
        if (InputSystem == null)
        {
            InputSystem = new InputSystem();
            WalkAction = new InputActionBuilder(InputSystem.Player.Move);
            RunAction = new InputActionBuilder(InputSystem.Player.RunFast);
            JumpAction = new InputActionBuilder(InputSystem.Player.Jump);
            InteractAction = new InputActionBuilder(InputSystem.Player.Interact);
            ZoomAction = new InputActionBuilder(InputSystem.Player.Zoom);
            EscAction = new InputActionBuilder(InputSystem.Screen.CursorLock);
            AltAction = new InputActionBuilder(InputSystem.Screen.CursorUnfocus);
            OpenInventoryAction = new InputActionBuilder(InputSystem.Player.OpenInventory);
            MouseAction = new InputActionBuilder(InputSystem.Player.Mouse);
            SwitchWeaponAction = new InputActionBuilder(InputSystem.Player.SwitchWeapon);
            FireAction = new InputActionBuilder(InputSystem.Player.Fire);
        }
    }

    private void Start()
    {
        EnableAllInputs();
    }

    private void OnDisable()
    {
        DisableAllInputs();
    }

    public void OnEnable()
    {
        EnableAllInputs();
    }

    public void EnableAllInputs()
    {
        WalkAction.Enable();
        RunAction.Enable();
        JumpAction.Enable();
        InteractAction.Enable();
        ZoomAction.Enable();
        EscAction.Enable();
        AltAction.Enable();
        MouseAction.Enable();
        OpenInventoryAction.Enable();
        SwitchWeaponAction.Enable();
        FireAction.Enable();
    }

    public void DisableAllInputs()
    {
        WalkAction.Disable();
        RunAction.Disable();
        JumpAction.Disable();
        InteractAction.Disable();
        ZoomAction.Disable();
        EscAction.Disable();
        AltAction.Disable();
        MouseAction.Disable();
        OpenInventoryAction.Disable();
        SwitchWeaponAction.Disable();
        FireAction.Disable();
    }
}