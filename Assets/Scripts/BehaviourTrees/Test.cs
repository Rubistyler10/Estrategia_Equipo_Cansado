using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    bool foo = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current != null &&
            Mouse.current.rightButton.isPressed &&
            !foo)
        {
            foo = true;
            Debug.Log("Before Coroutine [TEST]");
            StartCoroutine(TestFunc());
            Debug.Log("After Coroutine [TEST]");
        }

        //Debug.Log("Outside key check [TEST]");
    }

    IEnumerator TestFunc()
    {
        int counter = 0;
        while (counter < 5)
        {
            Debug.Log($"Counter (before ++) = {counter} [TEST]");
            counter++;
            yield return null;
            Debug.Log($"Counter (after ++) = {counter} [TEST]");
        }

        yield return new WaitForSeconds(1);
        Debug.Log($"Counter (end of coroutine) = {counter} [TEST]");

    }
}
