using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "dialogue", menuName = "Dialogues")]
public class Dialogue : ScriptableObject
{
    public string conversantName;
    public List<string> dialogueLines;
    //public List<ItemConfig> inventoryItemList;
    bool hasGivenItems;
    public bool hasSaidDialogue;
    public bool isRepeatableDialogue;
 

    private void Awake()
    {
        //inventoryItemList = new List<ItemConfig>();
        hasGivenItems = false;
        hasSaidDialogue = false;
    }

    public void OnDialogueEnd()
    {
        //if (inventoryItemList != null && !hasGivenItems)
        //{
        //    foreach (ItemConfig item in inventoryItemList)
        //    {
        //        InventorySystem.instance.Add(item);
        //    }
        //    hasGivenItems = true;
        //}
        //hasSaidDialogue = true;
        
    }
}
