using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationEnemyController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public int curHealth;

    [Header("Navigation")]
    public NavigationGraph navigationGraph;
    public Transform target; // Player target

    [Header("Movement")]
    public float speed = 3f;
    public float pathRetryDelay = 0.5f;
    public float detectionRange = 5.0f;
    public float lowHealthThreshold = 0.2f; // 20% of max health

    [Header("References")]
    public SpriteRenderer spriteRenderer;

    // State machine
    public enum StateMachine
    {
        Patrol,
        Engage,
        Evade
    }
    public StateMachine currentState;

    // Movement
    private Vector3 lastPosition;
    private Vector3 currentDirection;
    private Vector3 goalPosition;
    private float pathRetryTimer;
    private bool useRandomMovement = false;
    private Vector3 randomMoveTarget;
    private float randomMoveTimer = 0f;

    private void Start()
    {
        // Initialize health
        curHealth = maxHealth;

        // Get references
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        lastPosition = transform.position;

        // Find player if not set
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("No player found for enemy to target!");
            }
        }

        // Find navigation graph if not set
        if (navigationGraph == null)
        {
            navigationGraph = FindObjectOfType<NavigationGraph>();
            if (navigationGraph == null)
            {
                Debug.LogError("No NavigationGraph found in scene! Enemy pathfinding will not work.");
            }
        }

        // Start in patrol state
        currentState = StateMachine.Patrol;
    }

    private void Update()
    {
        if (navigationGraph == null || target == null)
            return;

        // Update state based on conditions
        UpdateState();

        // Update goal position based on current state
        UpdateGoalPosition();

        // Move towards goal
        MoveTowardsGoal();

        // Update sprite direction
        UpdateSpriteDirection();
    }

    private void UpdateState()
    {
        if (target == null)
            return;

        bool playerSeen = Vector2.Distance(transform.position, target.position) < detectionRange;
        float healthPercentage = (float)curHealth / maxHealth;

        // State transitions
        if (!playerSeen && currentState != StateMachine.Patrol && healthPercentage > lowHealthThreshold)
        {
            // Lost sight of player and health is good -> Patrol
            currentState = StateMachine.Patrol;
        }
        else if (playerSeen && currentState != StateMachine.Engage && healthPercentage > lowHealthThreshold)
        {
            // Spotted player and health is good -> Engage
            currentState = StateMachine.Engage;
        }
        else if (currentState != StateMachine.Evade && healthPercentage <= lowHealthThreshold)
        {
            // Health is low -> Evade
            currentState = StateMachine.Evade;
        }
    }

    private void UpdateGoalPosition()
    {
        pathRetryTimer -= Time.deltaTime;

        if (pathRetryTimer <= 0)
        {
            pathRetryTimer = pathRetryDelay;

            switch (currentState)
            {
                case StateMachine.Patrol:
                    SetPatrolGoal();
                    break;
                case StateMachine.Engage:
                    SetEngageGoal();
                    break;
                case StateMachine.Evade:
                    SetEvadeGoal();
                    break;
            }
        }
    }

    private void SetPatrolGoal()
    {
        // Get random position within reasonable range
        Vector2 randomOffset = Random.insideUnitCircle * 10f;
        goalPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

        // Make sure goal is above ground if possible
        RaycastHit2D hit = Physics2D.Raycast(goalPosition, Vector2.down, 10f, navigationGraph.groundLayer);
        if (hit.collider != null)
        {
            goalPosition = new Vector3(goalPosition.x, hit.point.y + 0.5f, goalPosition.z);
        }
    }

    private void SetEngageGoal()
    {
        if (target != null)
        {
            goalPosition = target.position;
        }
    }

    private void SetEvadeGoal()
    {
        if (target != null)
        {
            // Move away from player
            Vector2 awayDirection = ((Vector2)transform.position - (Vector2)target.position).normalized;
            Vector2 awayPosition = (Vector2)transform.position + awayDirection * 10f;

            // Try to find a valid position on ground
            RaycastHit2D hit = Physics2D.Raycast(awayPosition, Vector2.down, 10f, navigationGraph.groundLayer);
            if (hit.collider != null)
            {
                goalPosition = new Vector3(awayPosition.x, hit.point.y + 0.5f, 0);
            }
            else
            {
                goalPosition = new Vector3(awayPosition.x, awayPosition.y, 0);
            }
        }
    }

    private void MoveTowardsGoal()
    {
        if (navigationGraph == null)
            return;

        // Get movement action from navigation graph
        MovementAction action = navigationGraph.GetMovementAction(transform.position, goalPosition);

        // Save last position to calculate movement direction
        lastPosition = transform.position;

        // Apply movement based on action
        switch (action)
        {
            case MovementAction.MoveLeft:
                transform.position += Vector3.left * speed * Time.deltaTime;
                break;

            case MovementAction.MoveRight:
                transform.position += Vector3.right * speed * Time.deltaTime;
                break;

            case MovementAction.Idle:
                // If we're stuck in idle, occasionally use fallback random movement
                randomMoveTimer -= Time.deltaTime;
                if (!useRandomMovement)
                {
                    useRandomMovement = true;
                    Vector2 randomDir = Random.insideUnitCircle.normalized;
                    randomMoveTarget = transform.position + new Vector3(randomDir.x, randomDir.y, 0) * 2f;
                    randomMoveTimer = Random.Range(0.5f, 1.5f);
                }

                if (randomMoveTimer > 0)
                {
                    transform.position = Vector3.MoveTowards(transform.position, randomMoveTarget, speed * 0.5f * Time.deltaTime);
                }
                else
                {
                    useRandomMovement = false;
                }
                break;
        }

        // Calculate movement direction for sprite flipping
        if (Vector3.Distance(lastPosition, transform.position) > 0.001f)
        {
            currentDirection = (transform.position - lastPosition).normalized;
        }
    }

    private void UpdateSpriteDirection()
    {
        if (currentDirection.magnitude > 0.001f && spriteRenderer != null)
        {
            if (currentDirection.x != 0)
            {
                spriteRenderer.flipX = currentDirection.x < 0;
            }
        }
    }

    // Method to handle damage taken
    public void TakeDamage(int damage)
    {
        curHealth -= damage;

        // Ensure health doesn't go below 0
        if (curHealth < 0)
            curHealth = 0;

        // Force state update immediately after taking damage
        UpdateState();

        // Handle death if health reaches 0
        if (curHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Add death logic here (e.g., play animation, drop items)
        // For now, just destroy the GameObject
        Destroy(gameObject);
    }

    // Debugging
    private void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize goal position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(goalPosition, 0.3f);

            // Draw line to goal
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, goalPosition);
        }
    }
}