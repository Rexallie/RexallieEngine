using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for TryParse

public class VariableManager : MonoBehaviour
{
    public static VariableManager Instance { get; private set; }

    // A dictionary to hold all our story variables by name.
    private Dictionary<string, object> variables = new Dictionary<string, object>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Core Variable Methods ---

    public void SetVariable(string name, object value)
    {
        variables[name] = value;
    }

    public T GetVariable<T>(string name, T defaultValue = default(T))
    {
        if (variables.ContainsKey(name) && variables[name] is T)
        {
            return (T)variables[name];
        }
        return defaultValue;
    }

    // --- Condition Evaluation ---

    // This is the core logic that will parse and evaluate conditions from your VNS script.
    public bool EvaluateCondition(string condition)
    {
        // Example conditions: "alice_points > 10", "met_bob == true", "item != sword"
        string[] parts = condition.Split(' ');
        if (parts.Length != 3)
        {
            Debug.LogError($"Invalid condition format: '{condition}'. Expected [variable] [operator] [value]");
            return false;
        }

        string varName = parts[0];
        string op = parts[1];
        string valueStr = parts[2];

        if (!variables.ContainsKey(varName))
        {
            // If a variable hasn't been set yet, we can treat it as 0 or false.
            variables[varName] = 0;
        }

        object varValue = variables[varName];

        // --- Handle Integer Comparisons ---
        if (varValue is int)
        {
            int varInt = (int)varValue;
            if (int.TryParse(valueStr, out int valInt))
            {
                switch (op)
                {
                    case "==": return varInt == valInt;
                    case "!=": return varInt != valInt;
                    case ">": return varInt > valInt;
                    case "<": return varInt < valInt;
                    case ">=": return varInt >= valInt;
                    case "<=": return varInt <= valInt;
                    default: return false;
                }
            }
        }

        // --- Handle Boolean Comparisons ---
        if (varValue is bool)
        {
            bool varBool = (bool)varValue;
            if (bool.TryParse(valueStr, out bool valBool))
            {
                switch (op)
                {
                    case "==": return varBool == valBool;
                    case "!=": return varBool != valBool;
                    default: return false;
                }
            }
        }

        // --- Handle String Comparisons ---
        if (varValue is string)
        {
            string varString = (string)varValue;
            switch (op)
            {
                case "==": return varString == valueStr;
                case "!=": return varString != valueStr;
                default: return false;
            }
        }

        Debug.LogError($"Could not evaluate condition: '{condition}'. Type mismatch or unknown operator.");
        return false;
    }

    // --- Save/Load Integration ---

    public List<VariableSaveData> GetVariableData()
    {
        var dataList = new List<VariableSaveData>();
        foreach (var pair in variables)
        {
            dataList.Add(new VariableSaveData
            {
                name = pair.Key,
                value = pair.Value.ToString(),
                type = pair.Value.GetType().Name
            });
        }
        return dataList;
    }

    public void RestoreVariableData(List<VariableSaveData> dataList)
    {
        variables.Clear();
        if (dataList == null) return;

        foreach (var data in dataList)
        {
            switch (data.type)
            {
                case "Int32":
                    variables[data.name] = int.Parse(data.value);
                    break;
                case "Boolean":
                    variables[data.name] = bool.Parse(data.value);
                    break;
                case "String":
                    variables[data.name] = data.value;
                    break;
                    // Add cases for other types like Single (float) if you need them.
            }
        }
    }
}