using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace ND_DrawTrello.Editor
{
    public struct SearchContextElement
    {
        public object target { get; private set; }
        public string title { get; private set; }

        public SearchContextElement(object target, string title)
        {
            this.target = target;
            this.title = title;
        }
    }

    public class ND_DrawTrelloSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        public ND_DrawTrelloView view;
        public VisualElement target;

        public static List<SearchContextElement> elements;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> tree = new List<SearchTreeEntry>();
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Nodes"), 0));

            elements = new List<SearchContextElement>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    var attribute = type.GetCustomAttribute<NodeInfoAttribute>();
                    if (attribute != null)
                    {
                        NodeInfoAttribute att = (NodeInfoAttribute)attribute;
                        var node = Activator.CreateInstance(type);

                        if (string.IsNullOrEmpty(att.menuItem)) continue;

                        elements.Add(new SearchContextElement(node, att.menuItem));
                    }
                }
            }

            // Sort by name
            elements.Sort((entry1, entry2) =>
            {
                string[] splits1 = entry1.title.Split('/');
                string[] splits2 = entry2.title.Split('/');
                for (int i = 0; i < splits1.Length; i++)
                {
                    if (i >= splits2.Length)
                    {
                        return 1;
                    }

                    int value = splits1[i].CompareTo(splits2[i]);
                    if (value != 0)
                    {
                        // Make sure that leaves go before nodes
                        if (splits1.Length != splits2.Length && (i == splits1.Length - 1 || i == splits2.Length - 1))
                            return splits1.Length < splits2.Length ? 1 : -1;

                        return value;
                    }
                }

                return 0;
            });

            // Add group entries
            List<string> groups = new List<string>();

            foreach (SearchContextElement element in elements)
            {
                string[] entryTitle = element.title.Split('/');
                string groupName = "";

                for (int i = 0; i < entryTitle.Length - 1; i++)
                {
                    groupName += entryTitle[i];
                    if (!groups.Contains(groupName))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(entryTitle[i]), i + 1));
                        groups.Add(groupName);
                    }
                    groupName += "/";
                }

                // Add final entry
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(entryTitle.Last()));
                entry.level = entryTitle.Length;
                entry.userData = new SearchContextElement(element.target, element.title);
                tree.Add(entry);
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Debug.Log(view.window.position.position);
            // Convert screen mouse position to local graph position
            var windowMousePosition = view.ChangeCoordinatesTo(
                view, context.screenMousePosition - view.window.position.position);
            var graphMousePosition = view.contentViewContainer.WorldToLocal(windowMousePosition);

            // Retrieve the selected search element
            SearchContextElement element = (SearchContextElement)SearchTreeEntry.userData;

            // Instantiate and position the node
            Node node = (Node)element.target;
            node.SetPosition(new Rect(graphMousePosition, new Vector2()));

            // Add node to the graph
            view.AddNewNode(node);

            return true;
        }

    }
}
 