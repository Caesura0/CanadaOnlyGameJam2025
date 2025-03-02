using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoaderCallback : MonoBehaviour
{
    bool isFirstUpdate = true;

    private void Update()
    {
        if (isFirstUpdate)
        {
            Debug.Log("loadercallback");
            isFirstUpdate= false;
            Loader.LoaderCallback();
        }
    }
}
