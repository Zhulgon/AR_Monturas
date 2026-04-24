using UnityEngine;

public class ARScene : MonoBehaviour
{
    [SerializeField] private GameObject startModel;
    private int modelsCount;
    private int indexCurrentModel;

    void Start()
    {
        modelsCount = transform.childCount;
        if (modelsCount == 0)
        {
            indexCurrentModel = 0;
            return;
        }

        if (startModel != null)
        {
            indexCurrentModel = startModel.transform.GetSiblingIndex();
        }
        else
        {
            indexCurrentModel = 0;
            transform.GetChild(indexCurrentModel).gameObject.SetActive(true);
        }
    }

    public void ChangeARModel(int index)
    {
        transform.GetChild(indexCurrentModel).gameObject.SetActive(false);
        int newIndex = indexCurrentModel + index;
        if (newIndex < 0)
        {
            newIndex = modelsCount -1;
        }
        else if(newIndex > modelsCount -1)
        {
            newIndex = 0;
        }

        GameObject newModel = transform.GetChild(newIndex).gameObject;
        newModel.SetActive(true);

        indexCurrentModel = newModel.transform.GetSiblingIndex();

    }

}
