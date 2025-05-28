using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomItem", menuName = "Custom/Item", order = 1)]
public class CustomItem : ScriptableObject
{
    public string itemName = "New Item";
    public float Lol = 5f;
}