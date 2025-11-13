using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{
    private PhysicalObject physicalObject;
    private CharacterStatus character;
    private WeaponManagerScript weapons;
    private Vector2 moveInput;
    private bool jumpPressed;
    private Vector3 cursorPos;
    private bool isFiring = false;
    
    // Start is called before the first frame update
    private void Awake()
    {
        physicalObject = GetComponent<PhysicalObject>();
        character = GetComponent<CharacterStatus>();
        weapons = character.GetWeapon();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        AimUpdate();
        FireUpdate();
    }
    
    void AimUpdate()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        if (float.IsFinite(mousePos.x) && float.IsFinite(mousePos.y))
        {
            cursorPos = Camera.main.ScreenToWorldPoint(mousePos);
            cursorPos.z = 0f;
        }

        // Direction from player to mouse
        Vector3 direction = (cursorPos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        weapons.UpdateAim(angle);
        physicalObject.FlipSprite(angle);
    }
    
    void OnMove(InputValue inputValue)
    {
        // moveInput = context.ReadValue<Vector2>();
        moveInput = inputValue.Get<Vector2>();
        jumpPressed = moveInput.y > 0;
        physicalObject.SetInput(moveInput, jumpPressed);
    }

    void OnShoot(InputValue inputValue)
    {
        isFiring = inputValue.isPressed;
    }

    private void FireUpdate()
    {
        weapons.TriggerShoot(isFiring);
    }
    
    private void OnSwitchWeaponUp()
    {
        weapons.Switch(1);
    }

    private void OnSwitchWeaponDown()
    {
        weapons.Switch(-1);
    }
}
