using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTextScript : MonoBehaviour
{
    [SerializeField] private float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += speed * Vector3.up * Time.deltaTime;
    }
}
