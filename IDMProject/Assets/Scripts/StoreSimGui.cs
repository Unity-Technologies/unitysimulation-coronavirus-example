using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoreSimGui : MonoBehaviour
{
    public StoreSimulation storeSimulation;

    public Slider numShoppersSlider;
    public Slider numContagiousSlider;

    // Start is called before the first frame update
    void Start()
    {
        numShoppersSlider.value = storeSimulation.DesiredNumShoppers;
        numContagiousSlider.maxValue = numShoppersSlider.value;
        numContagiousSlider.value = storeSimulation.DesiredNumContagious;
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
    }

    public void OnNumContagiousChanged()
    {
        storeSimulation.DesiredNumContagious = (int)numContagiousSlider.value;
    }
}
