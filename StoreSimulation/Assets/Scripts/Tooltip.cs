using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Tooltip : MonoBehaviour
{
    public TMP_Text tooltipText;
    public RectTransform tooltipBackgroundRect;
    public float textPadding = 4f;
    public static Tooltip instance;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        //ShowTooltip("As the owner of our fictitious store, there are a number of parameters associated with the operation of the store that we can adjust to try to manage virus exposure. ");
        gameObject.SetActive(false);
    }

    private void Update()
    {
        SetTooltipPos();
    }

    public void ShowTooltip(string tooltipString)
    {
        gameObject.SetActive(true);
        tooltipText.text = tooltipString;

        var preferredSize = tooltipText.GetPreferredValues();
        Vector2 backgroundSize = new Vector2(tooltipText.GetComponent<RectTransform>().rect.width + textPadding * 2f, preferredSize.y + textPadding * 2f);
        tooltipBackgroundRect.sizeDelta = backgroundSize;
        SetTooltipPos();
        
    }

    void SetTooltipPos()
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), Input.mousePosition, Camera.main, out localPoint);
        localPoint.y -= (tooltipText.textBounds.size.y / 2f) - 10f;
        transform.localPosition = localPoint;
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    /*public static void ShowTooltip_Static(string tooltipString)
    {
        instance.ShowTooltip(tooltipString);
    }

    public static void HideTooltip_Static()
    {
        instance.HideTooltip();
    }*/
}
