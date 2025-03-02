using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader 
{

    private class LoadingMonoBehavior : MonoBehaviour
    {

    }

    public enum Scene
    {
        MainMenu,
        Gameplay,
        Loading, 

    }


    private static Action onLoaderCallback;
    private static AsyncOperation loadingAsyncOperation;

    public static void Load(Scene scene)
    {
        Debug.Log(scene.ToString());
        //this creates a method/function through the lambda that can then be called in the loader callback 
        onLoaderCallback = () =>
        {
            GameObject loadingGameObject = new GameObject("Loading Game Object");
            loadingGameObject.AddComponent<LoadingMonoBehavior>().StartCoroutine(LoadSceneAsync(scene));
            //stores the target scene to be loaded
            
        };
        //loads the loading scene, acts as a buffer for smoother transitions
        SceneManager.LoadScene(Scene.Loading.ToString());
    }


    static IEnumerator LoadSceneAsync(Scene scene)
    {
        yield return null;

        loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString());
        
        while(!loadingAsyncOperation.isDone)
        {
            yield return null;
        }
    }


    public static float GetLoadingProgress()
    {
        if(loadingAsyncOperation != null)
        {
            return loadingAsyncOperation.progress;
        }
        else
        {
            return 1f;
        }
    }

    public static void LoaderCallback()
    {
        //this gets triggered after the first update, lets the scene refresh
        //then it will excute the loader callback and load the target scene
        if (onLoaderCallback != null)
        {
            onLoaderCallback();
            onLoaderCallback = null;
        }
    }
}
