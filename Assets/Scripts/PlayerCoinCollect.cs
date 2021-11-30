using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerCoinCollect : MonoBehaviour
{
    private int _currCore;
    public Image[] _cores;

    private void Start()
    {
        _currCore = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Core"))
        {
            Destroy(collision.gameObject);
            _cores[_currCore].color = Color.white;
            _currCore++;
        }
    }
}
