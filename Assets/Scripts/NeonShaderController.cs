using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NeonShaderController : MonoBehaviour
{
    private TilemapRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<TilemapRenderer>();
    }

    void Update()
    {
        Color pulse = Color.Lerp(Color.magenta, Color.cyan, Mathf.PingPong(Time.time / 20f, 1f));
        spriteRenderer.material.SetColor("_BaseColor", pulse);
    }
}