using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Cache;
using System.Reflection;
using Newtonsoft.Json;
using Unigine;

[Component(PropertyGuid = "cca5106cffe7d0dbfdced07ee34fbdc515551460")]
public static class InspectorTypes
{
    [Serializable]
    public struct ComponentFields
    {
        [JsonProperty("name")] public string Name;

        [JsonProperty("value")] public string Value;

        [JsonProperty("type")] public string Type;

        public ComponentFields(string name, string value, string type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public ComponentFields(FieldInfo fieldInfo, object owner)
        {
            Name = fieldInfo.Name;
            Value = fieldInfo.GetValue(owner).ToString();
            Type = "Empty";
            switch (fieldInfo.FieldType.ToString())
            {
                case "System.Int32":
                    Type = "Integer";
                    break;
                case "System.Single":
                    Type = "Float";
                    break;
                case "System.String":
                    Type = "String";
                    break;
                default:
                    Log.Message($"ComponentFields() unhandled type: {fieldInfo.FieldType}");
                    break;
            }
        }
    }

    [Serializable]
    public class ComponentData
    {
        [JsonProperty("fields")] public List<ComponentFields> Fields;

        public ComponentData(List<ComponentFields> componentFields)
        {
            Fields = componentFields;
        }
    }
    
    [Serializable]
    public struct FieldValue
    {
        [JsonProperty("name")]
        public string Field;
        
        [JsonProperty("value")]
        public string Value;

        public FieldValue(string field, string value)
        {
            this.Field = field;
            this.Value = value;
        }
    };
}