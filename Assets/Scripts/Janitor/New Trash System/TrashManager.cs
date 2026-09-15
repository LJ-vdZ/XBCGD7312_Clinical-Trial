using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TrashManager : MonoBehaviour
{
    public List<TrashGroup> trashPiles;
    int firstTrashPile = 0;

    int randomTrashPile;

    public int trashAccumulationRate = 1;

    //public Button addTrashBtn;

    float timer = 5; //temp


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //addTrashBtn.onClick.AddListener(OnTrashButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {
        //Temp
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 5f;

            // Call your function here
            AddTrash();
        }
    }

    void OnTrashButtonClicked()
    {
        AddTrash();
    }
    public void AddTrash()
    {
        for(int i = 0; i  < trashAccumulationRate; i++)
        {
            List<TrashGroup> availablePiles = new List<TrashGroup>();
            foreach (TrashGroup pile in trashPiles)
            {
                if (!pile.IsFull())
                {
                    availablePiles.Add(pile);
                }
            }

            // Stop if all piles are full
            if (availablePiles.Count == 0)
            {
                Debug.Log("All trash piles are full!");
                return;
            }
            randomTrashPile = Random.Range(firstTrashPile, availablePiles.Count);
            availablePiles[randomTrashPile].addTrash();
        }
    }
}
