// --- START OF FILE DynamicEventDrawer.cs ---
// --- NHỚ ĐẶT FILE NÀY TRONG MỘT THƯ MỤC CÓ TÊN "Editor" ---

using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;

[CustomPropertyDrawer(typeof(DynamicEvent))]
public class DynamicEventDrawer : PropertyDrawer
{
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    // Dictionary này sẽ được tự động điền vào khi Editor khởi động.
    private static readonly Dictionary<Type, Type> TypeToParameterTypeMap;

    /// <summary>
    /// Static constructor này chạy một lần, dùng Reflection để tự động tìm
    /// tất cả các lớp con của GenericParameter<T> và xây dựng bản đồ ánh xạ.
    /// </summary>
    static DynamicEventDrawer()
    {
        TypeToParameterTypeMap = new Dictionary<Type, Type>();

        // Quét tất cả các assembly để tìm các lớp Parameter của chúng ta
        var parameterTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && IsSubclassOfGeneric(t, typeof(GenericParameter<>)));

        foreach (var paramType in parameterTypes)
        {
            var baseType = paramType.BaseType;
            if (baseType != null && baseType.IsGenericType)
            {
                var genericArgument = baseType.GetGenericArguments()[0];
                if (!TypeToParameterTypeMap.ContainsKey(genericArgument))
                {
                    TypeToParameterTypeMap.Add(genericArgument, paramType);
                }
            }
        }
    }
    
    private static bool IsSubclassOfGeneric(Type toCheck, Type generic)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }

    private struct MethodSelection
    {
        public Component TargetComponent;
        public MethodInfo Method;
        public SerializedProperty Property; 
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        string uniqueId = property.propertyPath;
        if (!foldoutStates.ContainsKey(uniqueId))
        {
            foldoutStates[uniqueId] = true;
        }

        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float currentY = position.y;

        Rect foldoutRect = new Rect(position.x, currentY, position.width, lineHeight);
        foldoutStates[uniqueId] = EditorGUI.Foldout(foldoutRect, foldoutStates[uniqueId], label, true);
        currentY += lineHeight + spacing;

        if (foldoutStates[uniqueId])
        {
            EditorGUI.indentLevel++;

            SerializedProperty eventNameProp = property.FindPropertyRelative("eventName");
            SerializedProperty targetProp = property.FindPropertyRelative("target");
            SerializedProperty methodNameProp = property.FindPropertyRelative("methodName");
            SerializedProperty genericParamsProp = property.FindPropertyRelative("genericParameters");

            EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, lineHeight), eventNameProp);
            currentY += lineHeight + spacing;

            EditorGUI.PropertyField(new Rect(position.x, currentY, position.width, lineHeight), targetProp);
            currentY += lineHeight + spacing;

            Component targetComponent = targetProp.objectReferenceValue as Component;

            Rect methodRect = new Rect(position.x, currentY, position.width, lineHeight);
            
            string buttonLabel = "No Function";
            MethodInfo selectedMethod = null;
            if (targetComponent != null && !string.IsNullOrEmpty(methodNameProp.stringValue))
            {
                var parameterTypes = GetParameterTypesFromSerializedProperty(genericParamsProp);
                selectedMethod = targetComponent.GetType().GetMethod(methodNameProp.stringValue, parameterTypes);

                if (selectedMethod != null)
                {
                    buttonLabel = $"{targetComponent.GetType().Name}/{MethodSignature(selectedMethod)}";
                }
                else {
                    buttonLabel = "Missing Function";
                }
            }

            if (EditorGUI.DropdownButton(methodRect, new GUIContent(buttonLabel), FocusType.Keyboard))
            {
                GenerateMethodMenu(property).ShowAsContext();
            }
            currentY += lineHeight + spacing;
            
            if (selectedMethod != null)
            {
                var parameters = selectedMethod.GetParameters();

                if (genericParamsProp.arraySize != parameters.Length)
                {
                     // Thay đổi này sẽ được xử lý trong OnMethodSelected
                }
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i >= genericParamsProp.arraySize) continue;
                    
                    SerializedProperty paramElement = genericParamsProp.GetArrayElementAtIndex(i);
                    var paramInfo = parameters[i];
                    
                    float paramHeight = EditorGUI.GetPropertyHeight(paramElement, true);
                    Rect paramPropertyRect = new Rect(position.x, currentY, position.width, paramHeight);
                    EditorGUI.PropertyField(paramPropertyRect, paramElement, new GUIContent(paramInfo.Name), true);
                    currentY += paramHeight + spacing;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }

    private GenericMenu GenerateMethodMenu(SerializedProperty property)
    {
        GenericMenu menu = new GenericMenu();
        
        SerializedProperty targetProp = property.FindPropertyRelative("target");
        Component targetComponent = targetProp.objectReferenceValue as Component;
        
        if (targetComponent == null)
        {
            menu.AddDisabledItem(new GUIContent("Assign a target component first"));
            return menu;
        }

        GameObject targetGameObject = targetComponent.gameObject;
        
        menu.AddItem(new GUIContent("No Function"),
                     false,
                     () => {
                        var prop = property.Copy();
                        prop.FindPropertyRelative("target").objectReferenceValue = null;
                        prop.FindPropertyRelative("methodName").stringValue = null;
                        prop.FindPropertyRelative("genericParameters").arraySize = 0;
                        prop.serializedObject.ApplyModifiedProperties();
                     });
        menu.AddSeparator("");

        var components = targetGameObject.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null) continue;
            
            var componentType = component.GetType();
            var methods = componentType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && !m.IsGenericMethod && m.GetParameters().All(p => TypeToParameterTypeMap.ContainsKey(p.ParameterType)))
                .OrderBy(m => m.Name);

            foreach (var method in methods)
            {
                string menuPath = $"{componentType.Name}/{MethodSignature(method)}";
                var selection = new MethodSelection { TargetComponent = component, Method = method, Property = property };
                menu.AddItem(new GUIContent(menuPath), false, OnMethodSelected, selection);
            }
        }
        
        return menu;
    }
    
    private void OnMethodSelected(object userData)
    {
        var selection = (MethodSelection)userData;
        var property = selection.Property.Copy();
        var targetProp = property.FindPropertyRelative("target");
        var methodNameProp = property.FindPropertyRelative("methodName");
        var genericParamsProp = property.FindPropertyRelative("genericParameters");
        
        targetProp.objectReferenceValue = selection.TargetComponent;
        methodNameProp.stringValue = selection.Method.Name;

        var parameters = selection.Method.GetParameters();
        genericParamsProp.arraySize = parameters.Length;
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramInfo = parameters[i];
            var paramElement = genericParamsProp.GetArrayElementAtIndex(i);
            
            if (TypeToParameterTypeMap.TryGetValue(paramInfo.ParameterType, out Type concreteParameterType))
            {
                Debug.Log(concreteParameterType.ToString());
                paramElement.managedReferenceValue = Activator.CreateInstance(concreteParameterType);
                // Gán tên cho tham số để hiển thị trên UI
                var nameProp = paramElement.FindPropertyRelative("parameterName");
                if (nameProp != null) nameProp.stringValue = paramInfo.Name;
            }
            else
            {
                paramElement.managedReferenceValue = null;
            }
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        string uniqueId = property.propertyPath;
        if (!foldoutStates.ContainsKey(uniqueId) || !foldoutStates[uniqueId])
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        float totalHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4; // Foldout, Name, Target, Method button

        SerializedProperty genericParamsProp = property.FindPropertyRelative("genericParameters");
        for (int i = 0; i < genericParamsProp.arraySize; i++)
        {
            totalHeight += EditorGUI.GetPropertyHeight(genericParamsProp.GetArrayElementAtIndex(i), true) + EditorGUIUtility.standardVerticalSpacing;
        }

        return totalHeight;
    }

    private string MethodSignature(MethodInfo method)
    {
        string paramStr = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({paramStr})";
    }

    private Type[] GetParameterTypesFromSerializedProperty(SerializedProperty genericParamsProp)
    {
        var types = new Type[genericParamsProp.arraySize];
        for (int i = 0; i < genericParamsProp.arraySize; i++)
        {
            var paramElement = genericParamsProp.GetArrayElementAtIndex(i);
            if(paramElement.managedReferenceValue is GenericParameter gp)
            {
                types[i] = gp.GetParameterType();
            }
            else
            {
                return null; // Không thể xác định kiểu
            }
        }
        return types;
    }
}


// --- END OF FILE DynamicEventDrawer.cs ---