using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoreSimGui : MonoBehaviour
{
    public StoreSimulation storeSimulation;

    public Slider numShoppersSlider;
    public Slider numContagiousSlider;

    public TMP_Text numShoppersText;
    public TMP_Text numContagiousText;

    // Start is called before the first frame update
    void Start()
    {
        numShoppersSlider.value = storeSimulation.DesiredNumShoppers;
        numShoppersText.text = storeSimulation.DesiredNumShoppers.ToString();
        numContagiousSlider.maxValue = numShoppersSlider.value;
        numContagiousSlider.value = storeSimulation.DesiredNumContagious;
        numContagiousText.text = storeSimulation.DesiredNumContagious.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetSim()
    {
        SceneManager.LoadScene(0);
    }

    public void OnNumShoppersChanged()
    {
        storeSimulation.DesiredNumShoppers = (int)numShoppersSlider.value;
        numContagiousSlider.maxValue = storeSimulation.DesiredNumShoppers;
        numShoppersText.text = storeSimulation.DesiredNumShoppers.ToString();
    }

    public void OnNumContagiousChanged()
    {
        storeSimulation.DesiredNumContagious = (int)numContagiousSlider.value;
        numContagiousText.text = storeSimulation.DesiredNumContagious.ToString();
    }
}
