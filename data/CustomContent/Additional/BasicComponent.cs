using System.Collections.Generic;
using System.Reflection;
using Unigine;
using Console = System.Console;

[Component(PropertyGuid = "8e9b17588d3432d72b56324ab112fd218379f40c")]
public class BasicComponent : Component
{
    [ParameterAsset (Filter = ".spl")]
	public AssetLink spline;

    public InspectorTypes.ComponentData GetComponentStructure()
    {
        var componentFields = new List<InspectorTypes.ComponentFields>();
        foreach (var field in Fields)
        {
            var fieldInfo = GetType().GetField(field.Field);
            componentFields.Add(new InspectorTypes.ComponentFields(fieldInfo, this));
        }
        var list = new InspectorTypes.ComponentData(componentFields);
        return list;
    }

    private List<InspectorTypes.FieldValue> Fields = new List<InspectorTypes.FieldValue>();

    private bool isInspected;

    private void Init()
    {
        RegisterAllFields();
    }
    
    protected void Startup()
    {
        RegisterAllFields();
    }

    private void RegisterAllFields()
    {
        var listOfFields = GetType().GetFields();
        foreach (var field in listOfFields)
        {
            Fields.Add(new InspectorTypes.FieldValue(field.Name, field.GetValue(this).ToString()));
        }
    }

    protected void Update()
    {
        if (isInspected)
        {
            CheckFieldChanges();
        }
    }

    private void CheckFieldChanges()
    {
        var listOfFields = GetType().GetFields();
        List<InspectorTypes.FieldValue> fieldsForRemove = null;
        List<InspectorTypes.FieldValue> fieldsForAdd = null;
        foreach (var newField in listOfFields)
        {
            foreach (var oldField in Fields)
            {
                if (newField.Name.Equals(oldField.Field))
                {
                    string newValue = newField.GetValue(this).ToString();
                    if (!newField.GetValue(this).ToString().Equals(oldField.Value))
                    {
                        // DebugCheckFieldChanges(newField, oldField, newValue);
                        if (fieldsForAdd == null)
                        {
                            fieldsForAdd = new List<InspectorTypes.FieldValue>();
                            fieldsForRemove = new List<InspectorTypes.FieldValue>();
                        }
                        fieldsForAdd.Add(new InspectorTypes.FieldValue(newField.Name, newValue));
                        fieldsForRemove.Add(oldField);
                    }
                }
            }
        }

        if (fieldsForAdd == null) return;
        foreach (var fieldValue in fieldsForRemove)
        {
            Fields.Remove(fieldValue);
        }
        foreach(var field in fieldsForAdd)
        {
            Fields.Add(field);
            ServerLogic.AddChanges(field);
        }
    }

    private void DebugCheckFieldChanges(FieldInfo newField, InspectorTypes.FieldValue oldField, string newValue)
    {
        Console.WriteLine($"Updated from {newField.Name}:{newValue} to {oldField.Field}:{oldField.Value}");
    }

    public void Subscribe()
    {
        isInspected = true;
    }

    public void Unsubscribe()
    {
        isInspected = false;
    }
}