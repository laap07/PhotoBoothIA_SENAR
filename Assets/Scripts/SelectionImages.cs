using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionImages : MonoBehaviour
{
    [SerializeField] List<Image> selections = new List<Image>();
    [SerializeField] private List<Sprite> selectionsGeneral = new List<Sprite>();
    public DzineAPI dzine;

    private void OnEnable()
    {
        SetImages();
    }
    public void SetImages()
    {
        if(dzine.isMan)
        {
            int count = 0;
            foreach(var _image in selections)
            {
                _image.sprite = selectionsGeneral[count];   
                count++;
            }
        }
        else
        {
            int count = 3;
            foreach (var _image in selections)
            {
                _image.sprite = selectionsGeneral[count];
                count++;
            }
        }
    }
    
}
