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
    public Slider maxTransmissionDistanceSlider;
    public Slider transmissionProbAtZeroDistanceSlider;
    public Slider transmissionProbAtMaxDistanceSlider;

    public TMP_Text numShoppersText;
    public TMP_Text numContagiousText;
    public TMP_Text maxTransmissionDistanceText;
    public TMP_Text transmissionProbAtZeroDistanceText;
    public TMP_Text transmissionProbAtMaxDistanceText;
    public TMP_Text transmissionProbAtMaxDistanceLabel;

    float meterToFoot = 3.28084f;
    float footToMeter = 0.3048f;
    string transmissionProbAtMaxDistanceLabelText = "Transmission Chance at {0}ft";

    // Start is called before the first frame update
    void Start()
    {
        numShoppersSlider.value = storeSimulation.DesiredNumShoppers;
        numShoppersText.text = storeSimulation.DesiredNumShoppers.ToString();
        numContagiousSlider.maxValue = numShoppersSlider.value;
        numContagiousSlider.value = storeSimulation.DesiredNumContagious;
        numContagiousText.text = storeSimulation.DesiredNumContagious.ToString();
        maxTransmissionDistanceSlider.value = storeSimulation.ExposureDistanceMeters * meterToFoot;
        maxTransmissionDistanceText.text = (storeSimulation.ExposureDistanceMeters * meterToFoot).ToString();
        transmissionProbAtZeroDistanceSlider.value = storeSimulation.ExposureProbabilityAtZeroDistance;
        transmissionProbAtZeroDistanceText.text = storeSimulation.ExposureProbabilityAtZeroDistance.ToString();
        transmissionProbAtMaxDistanceSlider.value = storeSimulation.ExposureProbabilityAtMaxDistance;
        transmissionProbAtMaxDistanceText.text = storeSimulation.ExposureProbabilityAtMaxDistance.ToString();
        transmissionProbAtMaxDistanceLabel.text = string.Format(transmissionProbAtMaxDistanceLabelText, maxTransmissionDistanceSlider.value);
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

    public void OnMaxTransmissionDistanceChanged()
    {
        storeSimulation.ExposureDistanceMeters = maxTransmissionDistanceSlider.value * footToMeter;
        maxTransmissionDistanceText.text = maxTransmissionDistanceSlider.value.ToString("0.00");
        transmissionProbAtMaxDistanceLabel.text = string.Format(transmissionProbAtMaxDistanceLabelText, maxTransmissionDistanceSlider.value);
    }

    public void OnTransmissionProbablityAtMinDistanceChanged()
    {
        storeSimulation.ExposureProbabilityAtZeroDistance = transmissionProbAtZeroDistanceSlider.value;
        transmissionProbAtZeroDistanceText.text = storeSimulation.ExposureProbabilityAtZeroDistance.ToString("0.00");
    }

    public void OnTransmissionProbablityAtMaxDistanceChanged()
    {
        storeSimulation.ExposureProbabilityAtMaxDistance = transmissionProbAtMaxDistanceSlider.value;
        transmissionProbAtMaxDistanceText.text = storeSimulation.ExposureProbabilityAtMaxDistance.ToString("0.00");
    }
}
