using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    public Image[] _core;
    public GameObject _ui;

    private void Awake()
    {
        _ui.SetActive(false);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player") && _core[0].color == Color.white)
        {
            _ui.SetActive(true);
            StartCoroutine(WaitThingy());
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);


        }
    }

    IEnumerator WaitThingy()
    {
        yield return new WaitForSeconds(50f);
    }
}
