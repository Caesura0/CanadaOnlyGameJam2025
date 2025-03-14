using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueCollider : MonoBehaviour
{
    DialogueTrigger dialogueTrigger;
    bool hasSaidDialogue;
    private void Start()
    {
        dialogueTrigger = GetComponent<DialogueTrigger>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasSaidDialogue)
        {
            hasSaidDialogue = true;
            dialogueTrigger.Interact(collision.gameObject);
        }
    }
}
