using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    int health;
    int maxHealth = 3;

    public static EventHandler OnPlayerDeath;
    bool isDead;

    [Header("Hit Feedback")]
    [SerializeField] private float invincibilityTime = 1.5f;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private int numberOfFlashes = 5;

    private bool isInvincible = false;
    private SpriteRenderer[] playerSprites;

    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private GameObject heartHolder;

    private void Awake()
    {
        // Get all sprite renderers in the player (handles multi-sprite characters)
        playerSprites = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        health = maxHealth;
        SpawnHearts();
    }

    public void TakeDamage(int damage)
    {
        // Check if player is currently invincible
        if (isInvincible)
            return;

        health = Mathf.Clamp(health - damage, 0, maxHealth);
        SpawnHearts();

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Start hit feedback and invincibility
            StartCoroutine(HitFeedbackRoutine());
        }
    }

    public void Die()
    {
        if (isDead) { return; }
        isDead = true;
        OnPlayerDeath?.Invoke(this, EventArgs.Empty);
    }

    void SpawnHearts()
    {
        foreach (Transform child in heartHolder.transform)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < health; i++)
        {
            Instantiate(heartPrefab, heartHolder.transform);
        }
    }

    IEnumerator HitFeedbackRoutine()
    {
        // Start invincibility
        isInvincible = true;

        // Flash effect - alternate between regular color and flash color
        for (int i = 0; i < numberOfFlashes; i++)
        {
            SetPlayerSpritesColor(hitFlashColor);
            yield return new WaitForSeconds(flashDuration);

            ResetPlayerSpritesColor();
            yield return new WaitForSeconds(flashDuration);
        }

        // Wait for the remaining invincibility time
        float remainingInvincibilityTime = invincibilityTime - (flashDuration * 2 * numberOfFlashes);
        if (remainingInvincibilityTime > 0)
        {
            yield return new WaitForSeconds(remainingInvincibilityTime);
        }

        // End invincibility
        isInvincible = false;
    }

    // Helper method to change player sprite colors
    private void SetPlayerSpritesColor(Color color)
    {
        foreach (SpriteRenderer sprite in playerSprites)
        {
            if (sprite != null)
                sprite.color = color;
        }
    }

    // Helper method to reset player sprite colors
    private void ResetPlayerSpritesColor()
    {
        foreach (SpriteRenderer sprite in playerSprites)
        {
            if (sprite != null)
                sprite.color = Color.white;
        }
    }

    // Additional methods for external access
    public bool IsInvincible()
    {
        return isInvincible;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentHealth()
    {
        return health;
    }
}