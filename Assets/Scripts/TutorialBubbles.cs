using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialBubbles : MonoBehaviour
{
    public Text _text;

    private void Start()
    {
        _text.gameObject.SetActive(false);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {

            Debug.Log("HOla");
            if(collision.gameObject.name == "Bubble1")
            {
                _text.text = "Use WASD to move and SPACE to jump!";
            }

            _text.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            _text.gameObject.SetActive(false);
        }
    }
}
