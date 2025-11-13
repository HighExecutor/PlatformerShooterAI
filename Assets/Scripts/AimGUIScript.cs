using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimGUIScript : MonoBehaviour
{
    public Texture2D crosshairTexture; // Crosshair image
    public Vector2 crosshairSize = new Vector2(32, 32); // Size of the crosshair

    private void Start()
    {
        // Hide the default system cursor
        Cursor.visible = false;
    }

    private void OnGUI()
    {
        if (crosshairTexture != null)
        {
            // Get the mouse position
            Vector3 mousePosition = Input.mousePosition;

            // Convert mouse position for GUI space
            Vector2 crosshairPosition = new Vector2(
                mousePosition.x - crosshairSize.x / 2,
                Screen.height - mousePosition.y - crosshairSize.y / 2
            );

            // Draw the crosshair at the mouse position
            GUI.DrawTexture(new Rect(crosshairPosition, crosshairSize), crosshairTexture);
        }
    }
}
