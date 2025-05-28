using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class CustomListEditor : EditorWindow
{
    private Foldout rootFoldout;
    private VisualTreeAsset customItemVisualTree;

    [MenuItem("Window/Custom List Editor")]
    public static void ShowWindow()
    {
        CustomListEditor window = GetWindow<CustomListEditor>();
        window.titleContent = new GUIContent("Custom List Editor");
        window.minSize = new Vector2(300, 400);
    }

    private void CreateGUI()
    {
        // Load main UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/USS/ListTest.uxml");
        if (visualTree == null)
        {
            Debug.LogError("Could not load UXML file at Assets/Editor/ListElement.uxml");
            return;
        }

        // Load CustomItem UXML
        customItemVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/USS/CustomItem.uxml");
        if (customItemVisualTree == null)
        {
            Debug.LogError("Could not load UXML file at Assets/Editor/CustomItem.uxml");
            return;
        }

        // Apply main UXML to window
        visualTree.CloneTree(rootVisualElement);

        // Find root foldout
        rootFoldout = rootVisualElement.Q<Foldout>(className: "list-element-foldout");

        // Add button to create new ScriptableObject
        var addButton = new Button(() => AddScriptableObjectItem()) { text = "Add Custom Item" };
        addButton.style.marginTop = 10;
        addButton.style.marginLeft = 10;
        rootVisualElement.Add(addButton);

        // Setup existing list items
        SetupListItems();

        // Configure foldouts
        var foldouts = rootVisualElement.Query<Foldout>(className: "list-element-foldout").ToList();
        foreach (var foldout in foldouts)
        {
            foldout.RegisterValueChangedCallback(evt =>
            {
                Debug.Log($"Foldout '{foldout.text}' toggled: {evt.newValue}");
            });
        }
    }

    private void SetupListItems()
    {
        var items = rootVisualElement.Query<VisualElement>(className: "list-element-item").ToList();
        foreach (var item in items)
        {
            item.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Clear previous selection
                var previousSelected = rootVisualElement.Query<VisualElement>(className: "selected").ToList();
                foreach (var prev in previousSelected)
                {
                    prev.RemoveFromClassList("selected");
                }

                // Select current item
                item.AddToClassList("selected");
                Debug.Log($"Selected: {item.Q<Label>().text}");
            });
        }
    }

    private void AddScriptableObjectItem()
    {
        // Create new ScriptableObject
        var newItem = ScriptableObject.CreateInstance<CustomItem>();
        newItem.itemName = $"Custom Item {rootFoldout.childCount + 1}";
        newItem.Lol = 5f;

        // Create asset in project
        string path = $"Assets/USS/CustomItem_{System.Guid.NewGuid()}.asset";
        AssetDatabase.CreateAsset(newItem, path);
        AssetDatabase.SaveAssets();

        // Create visual element from CustomItem UXML
        var itemElement = customItemVisualTree.CloneTree();
        var container = itemElement.Q<VisualElement>(className: "scriptable-object-item");

        // Bind itemName
        var label = itemElement.Q<Label>(className: "scriptable-object-label");
        if (label != null)
        {
            label.text = newItem.itemName;
        }

        // Bind Lol field
        var floatField = itemElement.Q<FloatField>(className: "scriptable-object-float");
        if (floatField != null)
        {
            floatField.value = newItem.Lol;
            floatField.RegisterValueChangedCallback(evt =>
            {
                newItem.Lol = evt.newValue;
                EditorUtility.SetDirty(newItem);
                AssetDatabase.SaveAssets();
                Debug.Log($"Updated Lol for {newItem.itemName} to {newItem.Lol}");
            });
        }

        // Add selection handling
        if (container != null)
        {
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                var previousSelected = rootVisualElement.Query<VisualElement>(className: "selected").ToList();
                foreach (var prev in previousSelected)
                {
                    prev.RemoveFromClassList("selected");
                }
                container.AddToClassList("selected");
                Debug.Log($"Selected ScriptableObject: {newItem.itemName}, Lol: {newItem.Lol}");
            });
        }

        // Add to root foldout
        rootFoldout.Add(itemElement);
    }
}