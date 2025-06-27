using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Sirenix.Serialization;

[Serializable]
public class DynamicEvent
{
    public string eventName;
    public Component target;
    public string methodName;

    [OdinSerialize]
    public List<GenericParameter> genericParameters = new List<GenericParameter>();

    public void Invoke()
    {
        if (target == null)
        {
            Debug.LogError($"DynamicEvent '{eventName}': Target component is null.", target);
            return;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            Debug.LogError($"DynamicEvent '{eventName}': Method name is not specified.", target);
            return;
        }
        
        var paramTypes = genericParameters.Select(p => p.GetParameterType()).ToArray();
        var paramValues = genericParameters.Select(p => p.GetValue()).ToArray();

        MethodInfo methodInfo = target.GetType().GetMethod(methodName, paramTypes);

        if (methodInfo != null)
        {
            methodInfo.Invoke(target, paramValues);
        }
        else
        {
            var typesStr = string.Join(", ", paramTypes.Select(t => t.Name));
            Debug.LogError($"DynamicEvent '{eventName}': Method '{methodName}({typesStr})' not found on component '{target.GetType().Name}'.", target);
        }
    }
}