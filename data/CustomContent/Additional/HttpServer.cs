using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unigine;
using Console = System.Console;

[Component(PropertyGuid = "fdfd8b2783ed46db9b611d783effd633")]
public static class HttpServer
{
    [Serializable]
    struct ObjectData
    {
        public string Field { get; }
        public string Value { get; }

        public ObjectData(string field, string value)
        {
            Field = field;
            Value = value;
        }
    }

    [Serializable]
    struct ObjectPackage
    {
        public string ObjectName { get; }

        public List<ObjectData> Data { get; }

        public ObjectPackage(string objectName)
        {
            ObjectName = objectName;
            Data = new List<ObjectData>();
        }
    }

    private static List<ObjectPackage> _data = new List<ObjectPackage>();

    private static HttpClient _httpClient;

    public static async Task JoinToLocalServer()
    {
        _httpClient = new HttpClient {BaseAddress = new Uri("http://127.0.0.1:8088")};
        var content = new FormUrlEncodedContent(new Dictionary<string, string>());
        var response = await _httpClient.PostAsync(_httpClient.BaseAddress + "initial", content);
        var responseString = await response.Content.ReadAsStringAsync();

        Console.WriteLine(responseString);
        Log.Message($"ResponseString: {responseString}\n");

        var cookies = response.Headers
            .SingleOrDefault(header => header.Key == "Set-Cookie").Value;

        string authValue = "";

        foreach (var cookie in cookies)
        {
            var splittedCookie = cookie.Split(';');
            foreach (var str in splittedCookie)
            {
                if (!str.Contains("auth-example="))
                {
                    continue;
                }

                authValue = str.Replace("auth-example=", "");
                goto jump;
            }
        }

        jump:
        Log.Message($"Cookie: {authValue} \n");
    }

    public static void CloseConnection()
    {
        if (_httpClient == null)
        {
            return;
        }

        // TODO: More checks
        var response = _httpClient.GetAsync(_httpClient.BaseAddress + "close");
        var responseString = response.Result.Content.ReadAsStringAsync();
        Console.WriteLine($"Connection closing {responseString}");
    }

    public static void SendData()
    {
        if (_httpClient == null)
        {
            return;
        }
        var values = new Dictionary<string, string>();
        values.Add("Data", JsonConvert.SerializeObject(_data));
        var content = new FormUrlEncodedContent(values);
        _httpClient.PostAsync(_httpClient.BaseAddress + "update", content);
    }

    public static void AddData(object obj, string objectName, IEnumerable<FieldInfo> fields)
    {
        var item = new ObjectPackage(objectName);
        foreach (var field in fields)
        {
            item.Data.Add(new ObjectData(field.Name, field.GetValue(obj).ToString()));
        }

        _data.Add(item);
    }
}