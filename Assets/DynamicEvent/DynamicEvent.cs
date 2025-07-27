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
<<<<<<< HEAD

    private void ClearMethodSelection()
    {
        methodName = null;
        genericParameters.Clear();
    }

    private void OnMethodSelected(string newMethodName)
    {
        genericParameters.Clear();

        if (target == null || string.IsNullOrEmpty(newMethodName)) return;

        var selectedMethod = target.GetType()
                               .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                               .FirstOrDefault(m => MethodSignature(m) == newMethodName);

        if (selectedMethod == null)
        {
             selectedMethod = target.GetType()
                               .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                               .FirstOrDefault(m => MethodSignature(m) == newMethodName);
        }

        if (selectedMethod == null) return;

        foreach (var paramInfo in selectedMethod.GetParameters())
        {
            Type genericParamType = typeof(GenericParameter<>).MakeGenericType(paramInfo.ParameterType);
            GenericParameter newParam = (GenericParameter)Activator.CreateInstance(genericParamType);
            newParam.parameterName = paramInfo.Name;
            genericParameters.Add(newParam);
        }
    }

    private IEnumerable<ValueDropdownItem<string>> GetAvailableMethods()
    {
        if (target == null) yield break;

        yield return new ValueDropdownItem<string>("No Function", null);

        var methods = target.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && !m.IsGenericMethod && m.GetParameters().All(p => IsTypeSupported(p.ParameterType)))
            .OrderBy(m => m.Name);

        foreach (var method in methods)
        {
            string signature = MethodSignature(method);
            yield return new ValueDropdownItem<string>(signature, signature);
        }
    }

    private string MethodSignature(MethodInfo method)
    {
        string paramStr = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName));
        return $"{method.Name}({paramStr})";
    }

    private bool IsTypeSupported(Type type)
{
    if (type.IsByRef) return false; // Out/ref parameters are not supported

    // Allow primitives, strings, and enums
    if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return true;

    // Allow common Unity structs
    if (type == typeof(Color) || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Quaternion) || type == typeof(Rect)) return true;

    // Allow types that derive from UnityEngine.Object (e.g., Transform, GameObject, Material)
    if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;
    
    // Allow any other type that has the [Serializable] attribute, which covers custom structs.
    // This allows Odin to attempt to draw it.
    if (type.IsSerializable) return true;

    return false;
}
=======
>>>>>>> parent of 7c3eac6 (Odin Animation Hub Version)
}