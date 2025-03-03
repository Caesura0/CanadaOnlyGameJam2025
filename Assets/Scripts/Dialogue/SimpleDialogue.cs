using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimpleDialogue : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] TextMeshProUGUI conversantName;

    [SerializeField] float textSpeed;



    [SerializeField] Dialogue defaultDialogue;

    Dialogue currentDialogue;

    int index = 1;

    public bool InDialogue { get; private set; }


    public static SimpleDialogue instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        gameObject.SetActive(false);
    }


    void Update()
    {
        if(currentDialogue != null && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (dialogueText.text == currentDialogue.dialogueLines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                dialogueText.text = currentDialogue.dialogueLines[index];
            }
        }

    }


    public void StartDialogue(Dialogue dialogue, string conversant)
    {
        InDialogue = true;
        gameObject.SetActive(true);
        index = 0;
        dialogueText.text = string.Empty;
        if(dialogue != null)
        {
            currentDialogue = dialogue;
        }
        else
        {
            currentDialogue = defaultDialogue;
        }

        conversantName.text = conversant;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        foreach(char c in currentDialogue.dialogueLines[index].ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        if(index < currentDialogue.dialogueLines.Count -1)
        {
            index++;
            dialogueText.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            currentDialogue.OnDialogueEnd();
            currentDialogue = null;
            InDialogue = false;
            gameObject.SetActive(false);
        }
    }

    IEnumerator Pause()
    {
        yield return new WaitForSeconds(1.5f);  
    }
}
