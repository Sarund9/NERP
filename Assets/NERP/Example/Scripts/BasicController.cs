using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class BasicController : MonoBehaviour
{
    [SerializeField]
    Rigidbody rb;

    [SerializeField, Range(1, 20f)]
    float speedMult = 10f;
    
    [SerializeField, Range(1, 20f)]
    float sensitivity = 5f;

    Vector3 localVel;

    private void OnValidate()
    {
        if (!rb)
            rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        OnValidate();
    }

    private void Update()
    {
        var x = Input.GetAxis("Horizontal");
        var y = Input.GetAxis("Vertical");
        
        var desired = new Vector3(x, 0, y);

        localVel = desired * speedMult;

        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.None ?
                CursorLockMode.Locked : CursorLockMode.None;
        }

        if (Cursor.lockState == CursorLockMode.None)
            return;

        var rot = Input.GetAxis("Mouse X");
        transform.Rotate(0, rot * 30 * sensitivity * Time.deltaTime, 0);
    }

    private void FixedUpdate()
    {
        var planar = transform.TransformDirection(localVel);

        rb.velocity = new(planar.x, rb.velocity.y, planar.z);
        
    }
}
