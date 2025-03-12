using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 3;
    public int curHealth;
    public Node currentNode;
    public List<Node> path = new List<Node>();
    public enum StateMachine
    {
        Patrol,
        Engage,
        Evade
    }
    public StateMachine currentState;
    public PlayerController player;
    public float speed = 3f;

    public SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;
    private Vector3 currentDirection;

    private void Start()
    {
        curHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        lastPosition = transform.position;
        player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        switch (currentState)
        {
            case StateMachine.Patrol:
                Patrol();
                break;
            case StateMachine.Engage:
                Engage();
                break;
            case StateMachine.Evade:
                Evade();
                break;
        }

        bool playerSeen = Vector2.Distance(transform.position, player.transform.position) < 5.0f;
        if (!playerSeen && currentState != StateMachine.Patrol && curHealth > (maxHealth * 20) / 100)
        {
            currentState = StateMachine.Patrol;
            path.Clear();
        }
        else if (playerSeen && currentState != StateMachine.Engage && curHealth > (maxHealth * 20) / 100)
        {
            currentState = StateMachine.Engage;
            path.Clear();
        }
        else if (currentState != StateMachine.Evade && curHealth <= (maxHealth * 20) / 100)
        {
            currentState = StateMachine.Evade;
            path.Clear();
        }

        CreatePath();
        UpdateSpriteDirection();
    }

    void Patrol()
    {
        if (path.Count == 0)
        {
            path = AStarManager.instance.GeneratePath(currentNode, AStarManager.instance.AllNodes()[Random.Range(0, AStarManager.instance.AllNodes().Length)]);
        }
    }

    void Engage()
    {
        if (path.Count == 0)
        {
            path = AStarManager.instance.GeneratePath(currentNode, AStarManager.instance.FindNearestNode(player.transform.position));
        }
    }

    void Evade()
    {
        if (path.Count == 0)
        {
            path = AStarManager.instance.GeneratePath(currentNode, AStarManager.instance.FindFurthestNode(player.transform.position));
        }
    }

    public void CreatePath()
    {
        if (path.Count > 0)
        {
            int x = 0;
            Vector3 targetPosition = new Vector3(path[x].transform.position.x, path[x].transform.position.y, -2);
            lastPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);


            if (Vector3.Distance(lastPosition, transform.position) > 0.001f)
            {
                currentDirection = (transform.position - lastPosition).normalized;
            }

            if (Vector2.Distance(transform.position, path[x].transform.position) < 0.1f)
            {
                currentNode = path[x];
                path.RemoveAt(x);
            }
        }
    }

    void UpdateSpriteDirection()
    {
        if (currentDirection.magnitude > 0.001f)
        {
            if (currentDirection.x != 0)
            {
                spriteRenderer.flipX = currentDirection.x < 0;
            }
        }
    }
}