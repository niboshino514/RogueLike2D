using Manager;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        // 次のステージ遷移
        StageManager test = StageManager.Instance;
        Debug.Log(test);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
