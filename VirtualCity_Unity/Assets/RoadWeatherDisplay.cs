using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class RoadWeatherDisplay : MonoBehaviour
{
    [Header("Colab / ngrok URL")]
    public string apiUrl = "https://xxxx.ngrok-free.app/weather/14034";   // <-- vaihda tämä!

    [Header("UI")]
    public TextMeshPro temperatureText;          // Vedä TextMeshPro tähän
    public string displayFormat = "Road temp\n{0:F1} °C";

    [Header("Asetukset")]
    public float updateInterval = 10f;           // sekuntia
    public bool showDebugLogs = true;

    void Start()
    {
        if (temperatureText == null)
            temperatureText = GetComponentInChildren<TextMeshPro>();

        StartCoroutine(UpdateLoop());
    }

    IEnumerator UpdateLoop()
    {
        while (true)
        {
            yield return StartCoroutine(FetchWeather());
            yield return new WaitForSeconds(updateInterval);
        }
    }

    IEnumerator FetchWeather()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (showDebugLogs)
                    Debug.LogWarning("Weather fetch failed: " + request.error);

                if (temperatureText != null)
                    temperatureText.text = "Ei dataa";
                yield break;
            }

            string json = request.downloadHandler.text;

            try
            {
                // Yksinkertainen parsinta (ei tarvita erillistä JSON-kirjastoa)
                float temp = ParseRoadTemperature(json);

                if (temperatureText != null)
                    temperatureText.text = string.Format(displayFormat, temp);

                if (showDebugLogs)
                    Debug.Log($"Road temperature updated: {temp:F1} °C");
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON parse error: " + e.Message);
                if (temperatureText != null)
                    temperatureText.text = "Virhe";
            }
        }
    }

    // Etsii TIE_1 arvon JSONista ilman ulkoisia kirjastoja
    float ParseRoadTemperature(string json)
    {
        // Etsitään "TIE_1" ja sen "value"
        int tie1Index = json.IndexOf("\"TIE_1\"");
        if (tie1Index == -1) throw new System.Exception("TIE_1 not found");

        int valueIndex = json.IndexOf("\"value\"", tie1Index);
        if (valueIndex == -1) throw new System.Exception("value not found");

        int colon = json.IndexOf(':', valueIndex);
        int start = colon + 1;

        // Ohitetaan välilyönnit
        while (start < json.Length && (json[start] == ' ' || json[start] == '\t'))
            start++;

        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-' || json[end] == '.'))
            end++;

        string numberStr = json.Substring(start, end - start);
        return float.Parse(numberStr, System.Globalization.CultureInfo.InvariantCulture);
    }
}