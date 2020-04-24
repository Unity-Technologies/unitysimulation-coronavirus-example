using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StoreSimGui : MonoBehaviour
{
    public StoreSimulation storeSimulation;

    public Slider numShoppersSlider;
    [FormerlySerializedAs("numContagiousSlider")]
    public Slider numInfectiousSlider;
    public Slider maxTransmissionDistanceSlider;
    public Slider transmissionProbAtZeroDistanceSlider;
    public Slider transmissionProbAtMaxDistanceSlider;
    public Slider numberOfRegistersSlider;
    public Slider shopperMovementSpeedSlider;
    public Slider minTransactionTimeSlider;
    public Slider maxTransactionTimeSlider;

    public TMP_Text numShoppersText;
    [FormerlySerializedAs("numContagiousText")]
    public TMP_Text numInfectiousText;
    public TMP_Text maxTransmissionDistanceText;
    public TMP_Text transmissionProbAtZeroDistanceText;
    public TMP_Text transmissionProbAtMaxDistanceText;
    public TMP_Text transmissionProbAtMaxDistanceLabel;
    public TMP_Text numberOfRegistersText;
    public TMP_Text shopperMovementSpeedText;
    public TMP_Text minTransactionTimeText;
    public TMP_Text maxTransactionTimeText;

    public Toggle oneWayAislesToggle;

    public TMP_Text healthyCustomersText;
    public TMP_Text exposedCustomersText;
    public TMP_Text exposedPercentageText;
    public TMP_Text totalRuntimeText;

    float meterToFoot = 3.28084f;
    float footToMeter = 0.3048f;
    string transmissionProbAtMaxDistanceLabelText = "Exposure Probability at {0} ft";
    string healthyCustomerCountLabelText = "Number of Healthy Shoppers: {0}";
    string exposedCustomerCountLabelText = "Number of Exposed Shoppers: {0}";
    string exposedPercentageLabelText = "Exposure Rate: {0}%";
    string runtimeLabelText = "Running Time: {0} seconds";

    int healthyCount;
    int exposedCount;

    float secondsSinceStart = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        storeSimulation.NumHealthyChanged += OnNumHealthyChanged;
        storeSimulation.NumContagiousChanged += NumExposedChanged;

        numShoppersSlider.value = storeSimulation.DesiredNumShoppers;
        numShoppersText.text = storeSimulation.DesiredNumShoppers.ToString();
        numInfectiousSlider.maxValue = storeSimulation.DesiredNumShoppers > 20 ? 20 : storeSimulation.DesiredNumShoppers;
        numInfectiousSlider.value = storeSimulation.DesiredNumInfectious;
        numInfectiousText.text = storeSimulation.DesiredNumInfectious.ToString();
        maxTransmissionDistanceSlider.value = storeSimulation.ExposureDistanceMeters * meterToFoot;
        maxTransmissionDistanceText.text = (storeSimulation.ExposureDistanceMeters * meterToFoot).ToString();
        transmissionProbAtZeroDistanceSlider.value = storeSimulation.ExposureProbabilityAtZeroDistance;
        transmissionProbAtZeroDistanceText.text = storeSimulation.ExposureProbabilityAtZeroDistance.ToString();
        transmissionProbAtMaxDistanceSlider.value = storeSimulation.ExposureProbabilityAtMaxDistance;
        transmissionProbAtMaxDistanceText.text = storeSimulation.ExposureProbabilityAtMaxDistance.ToString();
        transmissionProbAtMaxDistanceLabel.text = string.Format(transmissionProbAtMaxDistanceLabelText, maxTransmissionDistanceSlider.value);
        numberOfRegistersSlider.value = storeSimulation.NumberOfCountersOpen;
        numberOfRegistersText.text = storeSimulation.NumberOfCountersOpen.ToString();
        oneWayAislesToggle.isOn = storeSimulation.OneWayAisles;
        shopperMovementSpeedSlider.value = storeSimulation.ShopperSpeed;
        shopperMovementSpeedText.text = storeSimulation.ShopperSpeed.ToString("0.00");
        minTransactionTimeSlider.value = storeSimulation.MinPurchaseTime;
        minTransactionTimeSlider.minValue = 0.01f;
        minTransactionTimeSlider.maxValue = storeSimulation.MaxPurchaseTime;
        minTransactionTimeText.text = storeSimulation.MinPurchaseTime.ToString("0.00");
        maxTransactionTimeSlider.value = storeSimulation.MaxPurchaseTime;
        maxTransactionTimeSlider.minValue = storeSimulation.MinPurchaseTime;
        maxTransactionTimeSlider.maxValue = 10;
        maxTransactionTimeText.text = storeSimulation.MaxPurchaseTime.ToString("0.00");
    }

    // Update is called once per frame
    void Update()
    {
        secondsSinceStart += Time.deltaTime;
        UpdateTimeText();
    }

    public void ResetSim()
    {
        secondsSinceStart = 0;
        healthyCount = 0;
        exposedCount = 0;
        UpdateExposurePercent();
        UpdateTimeText();
        //SceneManager.LoadScene(0);
        
    }

    public void OnNumShoppersChanged()
    {
        storeSimulation.DesiredNumShoppers = (int)numShoppersSlider.value;
        numInfectiousSlider.maxValue = storeSimulation.DesiredNumShoppers > 20 ? 20 : storeSimulation.DesiredNumShoppers;
        numShoppersText.text = storeSimulation.DesiredNumShoppers.ToString();
    }

    public void OnNumInfectiousChanged()
    {
        storeSimulation.DesiredNumInfectious = (int)numInfectiousSlider.value;
        numInfectiousText.text = storeSimulation.DesiredNumInfectious.ToString();
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

    public void OnNumberOfRegistersChanged()
    {
        storeSimulation.NumberOfCountersOpen = (int)numberOfRegistersSlider.value;
        numberOfRegistersText.text = ((int)numberOfRegistersSlider.value).ToString();
    }

    public void OnOneWayAislesToggleChanged(bool val)
    {
        storeSimulation.OneWayAisles = oneWayAislesToggle.isOn;
    }

    public void OnShopperMovementSpeedChanged()
    {
        storeSimulation.ShopperSpeed = shopperMovementSpeedSlider.value;
        shopperMovementSpeedText.text = storeSimulation.ShopperSpeed.ToString("0.00");
    }

    public void OnMinTransactionTimeChanged()
    {
        storeSimulation.MinPurchaseTime = minTransactionTimeSlider.value;
        minTransactionTimeText.text = storeSimulation.MinPurchaseTime.ToString("0.00");
        maxTransactionTimeSlider.minValue = storeSimulation.MinPurchaseTime;
        if(maxTransactionTimeSlider.value < storeSimulation.MinPurchaseTime)
        {
            maxTransactionTimeSlider.value = storeSimulation.MinPurchaseTime;
        }
    }

    public void OnMaxTransactionTimeChanged()
    {
        storeSimulation.MaxPurchaseTime = maxTransactionTimeSlider.value;
        maxTransactionTimeText.text = storeSimulation.MaxPurchaseTime.ToString("0.00");
        minTransactionTimeSlider.maxValue = storeSimulation.MaxPurchaseTime;
        if(minTransactionTimeSlider.value >= storeSimulation.MaxPurchaseTime)
        {
            minTransactionTimeSlider.value = storeSimulation.MaxPurchaseTime;
        }
    }

    public void OnNumHealthyChanged(int count)
    {
        healthyCustomersText.text = string.Format(healthyCustomerCountLabelText, count);
        healthyCount = count;
        UpdateExposurePercent();
    }

    public void NumExposedChanged(int count)
    {
        exposedCustomersText.text = string.Format(exposedCustomerCountLabelText, count);
        exposedCount = count;
        UpdateExposurePercent();
    }

    public void UpdateExposurePercent()
    {
        var exposedPercent = ((float)exposedCount / (float)(healthyCount + exposedCount)) * 100;
        if(exposedCount == 0 && healthyCount == 0)
        {
            exposedPercent = 0;
        }
        exposedPercentageText.text = string.Format(exposedPercentageLabelText, exposedPercent.ToString("0.00"));
    }

    public void UpdateTimeText()
    {
        totalRuntimeText.text = string.Format(runtimeLabelText, secondsSinceStart.ToString("0.00"));
    }
}
