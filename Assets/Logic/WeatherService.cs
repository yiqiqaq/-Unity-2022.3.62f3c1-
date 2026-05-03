using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Core;

namespace Logic
{
    [Serializable]
    public class AMapWeatherResponse
    {
        public string status;
        public string info;
        public string infocode;
        public string count;
        public List<AMapWeatherLive> lives;
    }

    [Serializable]
    public class AMapWeatherLive
    {
        public string province;
        public string city;
        public string adcode;
        public string weather;
        public string temperature;
        public string winddirection;
        public string windpower;
        public string humidity;
        public string reporttime;
    }

    public class WeatherService : MonoBehaviour
    {
        public static WeatherService Instance { get; private set; }

        private const string AMapWebApiKey = "0b7cd3d027c59e9792f164b061d3e7d8"; // 高德开放平台 API Key
        private const string AMapSecurityKey = "f40c0e9878faa69be57d7696d73a0a4a"; // 高德开放平台 安全密钥 (JS API用)
        private const string DefaultCityAdcode = "310000"; // 默认为上海 adcode

        public AMapWeatherLive CurrentWeather { get; private set; }

        public event Action<AMapWeatherLive> OnWeatherUpdated;
        public event Action<string> OnWeatherFetchFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        public void FetchWeather(string adcode = DefaultCityAdcode)
        {
            StartCoroutine(GetWeatherRoutine(adcode));
        }

        private IEnumerator GetWeatherRoutine(string adcode)
        {
            string url = $"https://restapi.amap.com/v3/weather/weatherInfo?city={adcode}&key={AMapWebApiKey}&extensions=base";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                    webRequest.result == UnityWebRequest.Result.DataProcessingError || 
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorMsg = $"[WeatherService] Error: {webRequest.error}";
                    Debug.LogError(errorMsg);
                    OnWeatherFetchFailed?.Invoke(errorMsg);
                }
                else
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    try
                    {
                        AMapWeatherResponse response = JsonUtility.FromJson<AMapWeatherResponse>(jsonResponse);
                        if (response != null && response.status == "1" && response.lives != null && response.lives.Count > 0)
                        {
                            CurrentWeather = response.lives[0];
                            Debug.Log($"[WeatherService] Weather Update: {CurrentWeather.city} - {CurrentWeather.weather}, {CurrentWeather.temperature}°C");
                            OnWeatherUpdated?.Invoke(CurrentWeather);
                        }
                        else
                        {
                            string err = $"[WeatherService] Invalid response mapping or API error. Response content: {jsonResponse}";
                            Debug.LogError(err);
                            OnWeatherFetchFailed?.Invoke("天气服务返回异常。");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[WeatherService] JSON Parse error: {e.Message}");
                        OnWeatherFetchFailed?.Invoke("天气数据解析失败。");
                    }
                }
            }
        }
    }
}