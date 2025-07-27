// --- START OF FILE GenericParameter.cs ---

using System;
using System.Reflection;
using UnityEngine;

// Lớp GenericParameter<T> vẫn giữ nguyên
[Serializable]
public class GenericParameter<T> : GenericParameter
{
    public T constantValue;

    public override object GetValue()
    {
        if (useConstant)
        {
            return constantValue;
        }
        
        object sourceValue = GetValueFromSource();
        if (sourceValue == null) return default(T);
        
        if (sourceValue is T variable)
        {
            return variable;
        }
        
        try
        {
            return Convert.ChangeType(sourceValue, typeof(T));
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not convert dynamic parameter '{sourceFieldName}' of type {sourceValue.GetType().Name} to {typeof(T).Name}. Error: {e.Message}", sourceComponent);
            return default(T);
        }
    }

    public override Type GetParameterType()
    {
        return typeof(T);
    }
}

// Lớp trừu tượng GenericParameter cũng giữ nguyên
[Serializable]
public abstract class GenericParameter
{
    public string parameterName;
    public bool useConstant = true;
    public Component sourceComponent;
    public string sourceFieldName;

    public abstract object GetValue();
    public abstract Type GetParameterType();

    protected object GetValueFromSource()
    {
        // ... (giữ nguyên code của bạn)
        if (sourceComponent == null || string.IsNullOrEmpty(sourceFieldName))
        {
            Debug.LogError("Dynamic parameter source is not configured.", sourceComponent);
            return null;
        }

        Type sourceType = sourceComponent.GetType();

        PropertyInfo propInfo = sourceType.GetProperty(sourceFieldName, BindingFlags.Public | BindingFlags.Instance);
        if (propInfo != null && propInfo.CanRead)
        {
            return propInfo.GetValue(sourceComponent);
        }

        FieldInfo fieldInfo = sourceType.GetField(sourceFieldName, BindingFlags.Public | BindingFlags.Instance);
        if (fieldInfo != null)
        {
            return fieldInfo.GetValue(sourceComponent);
        }

        Debug.LogError($"Could not find public property or field named '{sourceFieldName}' on component '{sourceType.Name}'.", sourceComponent);
        return null;
    }
}

// ===================================================================
// == THÊM CÁC CLASS CON CỤ THỂ VÀO ĐÂY ==
// == Unity cần các class này để có thể serialize được. ==
// ===================================================================
//
[Serializable] public class StringParameter : GenericParameter<string> {}
[Serializable] public class FloatParameter : GenericParameter<float> {}
[Serializable] public class IntParameter : GenericParameter<int> {}
[Serializable] public class BoolParameter : GenericParameter<bool> {}
[Serializable] public class Vector3Parameter : GenericParameter<Vector3> {}
[Serializable] public class Vector2Parameter : GenericParameter<Vector2> {}
[Serializable] public class ColorParameter : GenericParameter<Color> {}
[Serializable] public class GameObjectParameter : GenericParameter<GameObject> {}
[Serializable] public class ComponentParameter : GenericParameter<Component> { }

[Serializable] public class TransformParameter : GenericParameter<Transform> {}

// Bạn cũng có thể thêm các kiểu tùy chỉnh của mình, miễn là chúng [Serializable]
[Serializable] public class DamageInfoParameter : GenericParameter<DamageInfo> {}