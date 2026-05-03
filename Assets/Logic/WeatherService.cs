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
        private const string DefaultCityAdcode = "340400"; // 淮南市 adcode

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
                    Debug.LogWarning($"[WeatherService] 网络请求失败: {webRequest.error}，使用默认天气数据");
                    ApplyFallbackWeather();
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
                            Debug.LogWarning($"[WeatherService] API 返回异常 ({response?.info})，使用默认天气数据");
                            ApplyFallbackWeather();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[WeatherService] JSON 解析失败: {e.Message}，使用默认天气数据");
                        ApplyFallbackWeather();
                    }
                }
            }
        }

        private void ApplyFallbackWeather()
        {
            CurrentWeather = new AMapWeatherLive
            {
                province = "安徽",
                city = "淮南",
                adcode = DefaultCityAdcode,
                weather = "晴",
                temperature = "22",
                winddirection = "东南风",
                windpower = "2",
                humidity = "55",
                reporttime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            Debug.Log("[WeatherService] 使用默认天气: 淮南 - 晴, 22°C");
            OnWeatherUpdated?.Invoke(CurrentWeather);
        }
    }
}