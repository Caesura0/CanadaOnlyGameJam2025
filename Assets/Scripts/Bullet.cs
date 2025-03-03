using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float timeBeforeSelfDestruct = 10f;

    


    public void Init()
    {
        Destroy(gameObject, timeBeforeSelfDestruct);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {


            if (collision.TryGetComponent(out EnemyHealth enemy))
            {
                enemy.TakeDamage(1);
            }
            Destroy(gameObject);
        }
    }


}
