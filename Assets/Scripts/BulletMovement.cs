using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletMovement : MonoBehaviour
{

    [SerializeField] private float _bulletSpeed;
    public Rigidbody2D _rb;
    // Start is called before the first frame update
    void Start()
    {
        _rb.velocity = transform.right * _bulletSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(gameObject);
    }

}
