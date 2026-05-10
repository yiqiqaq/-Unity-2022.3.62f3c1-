using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Core;
using UnityEngine;
using UnityEngine.Networking;

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

        private const string AMapWebApiKey = "654b15fcdf56b91e0c186dcd10928239";
        private const string AMapWebApiSecret = "593b16cded8e2fe7c965a41d9790d326";
        private const string DefaultCityAdcode = "340400"; // 默认为淮南 adcode
        private const string DefaultWeatherProvince = "安徽省";
        private const string DefaultWeatherCity = "淮南";
        private const string DefaultWeatherCondition = "晴";
        private const string DefaultWeatherTemperature = "22";

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
            DontDestroyOnLoad(gameObject);
        }

        public void FetchWeather(string adcode = DefaultCityAdcode)
        {
            StartCoroutine(GetWeatherRoutine(adcode));
        }

        private static AMapWeatherLive CreateFallbackWeather()
        {
            return new AMapWeatherLive
            {
                province = DefaultWeatherProvince,
                city = DefaultWeatherCity,
                adcode = DefaultCityAdcode,
                weather = DefaultWeatherCondition,
                temperature = DefaultWeatherTemperature,
                winddirection = "东南风",
                windpower = "2",
                humidity = "45",
                reporttime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private void UseFallbackWeather(string reason)
        {
            CurrentWeather = CreateFallbackWeather();
            Debug.LogWarning($"[WeatherService] {reason}，使用默认天气数据：{CurrentWeather.city} - {CurrentWeather.weather}, {CurrentWeather.temperature}°C");
            OnWeatherUpdated?.Invoke(CurrentWeather);
        }

        private static string DescribeApiError(AMapWeatherResponse response)
        {
            if (response == null)
            {
                return "空响应";
            }

            if (!string.IsNullOrEmpty(response.info) && !string.IsNullOrEmpty(response.infocode))
            {
                return $"{response.info} ({response.infocode})";
            }

            if (!string.IsNullOrEmpty(response.info))
            {
                return response.info;
            }

            if (!string.IsNullOrEmpty(response.infocode))
            {
                return response.infocode;
            }

            return "未知错误";
        }

        private static string BuildWeatherRequestUrl(string adcode)
        {
            SortedDictionary<string, string> parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "city", adcode },
                { "extensions", "base" },
                { "key", AMapWebApiKey },
                { "output", "json" }
            };

            string queryString = BuildQueryString(parameters);
            string signature = CreateSignature(parameters, AMapWebApiSecret);

            if (string.IsNullOrEmpty(signature))
            {
                return $"https://restapi.amap.com/v3/weather/weatherInfo?{queryString}";
            }

            return $"https://restapi.amap.com/v3/weather/weatherInfo?{queryString}&sig={UnityWebRequest.EscapeURL(signature)}";
        }

        private static string BuildQueryString(SortedDictionary<string, string> parameters)
        {
            StringBuilder builder = new StringBuilder();

            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                if (builder.Length > 0)
                {
                    builder.Append('&');
                }

                builder.Append(UnityWebRequest.EscapeURL(parameter.Key));
                builder.Append('=');
                builder.Append(UnityWebRequest.EscapeURL(parameter.Value));
            }

            return builder.ToString();
        }

        private static string CreateSignature(SortedDictionary<string, string> parameters, string secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                return string.Empty;
            }

            StringBuilder signatureSource = new StringBuilder();

            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                if (signatureSource.Length > 0)
                {
                    signatureSource.Append('&');
                }

                signatureSource.Append(parameter.Key);
                signatureSource.Append('=');
                signatureSource.Append(parameter.Value);
            }

            signatureSource.Append(secret);

            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signatureSource.ToString()));
                StringBuilder signature = new StringBuilder(hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    signature.Append(hash[i].ToString("x2"));
                }

                return signature.ToString();
            }
        }

        private IEnumerator GetWeatherRoutine(string adcode)
        {
            string url = BuildWeatherRequestUrl(adcode);

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                    webRequest.result == UnityWebRequest.Result.DataProcessingError || 
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorMsg = $"[WeatherService] 天气请求失败: {webRequest.error}";
                    Debug.LogWarning(errorMsg);
                    UseFallbackWeather("天气请求失败");
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
                            string info = DescribeApiError(response);
                            if (response != null && response.infocode == "10009")
                            {
                                UseFallbackWeather($"高德天气 Key 平台不匹配 ({info})，请在高德控制台改用 Web 服务 Key 或取消平台限制");
                            }
                            else
                            {
                                UseFallbackWeather($"天气服务返回异常 ({info})");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[WeatherService] JSON Parse error: {e.Message}");
                        UseFallbackWeather("天气数据解析失败");
                    }
                }
            }
        }
    }
}