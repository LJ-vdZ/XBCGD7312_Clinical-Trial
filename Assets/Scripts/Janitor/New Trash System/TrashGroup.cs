using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TrashGroup : MonoBehaviour
{
    public List<GameObject> trash = new List<GameObject>();
    int trashIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        trashIndex = 0;
        for (int i = 0; i < trash.Count; i++)
        {
            trash[i].SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool addTrash()
    {
        if(trashIndex < trash.Count)
        {
            trash[trashIndex].SetActive(true);
            trashIndex++;
            return true;
        }
        return false;
    }

    public bool IsFull()
    {
        return trashIndex >= trash.Count;
    }
}
