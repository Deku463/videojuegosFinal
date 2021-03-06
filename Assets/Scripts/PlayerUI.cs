using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUI : MonoBehaviour
{

    private int maxHealth = 100;
    private int currentHealth;

    public HealthBar healthBar;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    // Update is called once per frame
    void Update()
    {
        if(currentHealth == 0)
        {
            SceneManager.LoadScene(5);
        }
        if (Input.GetKeyDown(KeyCode.Q)) TakeDamage(20);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
    }

}
