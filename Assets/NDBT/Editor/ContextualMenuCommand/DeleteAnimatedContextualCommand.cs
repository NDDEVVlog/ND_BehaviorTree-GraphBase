// File: DeleteAnimatedContextualCommand.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace ND_DrawTrello.Editor
{
    public class DeleteAnimatedContextualCommand : IContextualMenuCommand
    {
        public bool CanExecute(ContextualMenuPopulateEvent evt, ND_DrawTrelloView graphView)
        {
            return graphView.selection.Count > 0 &&
                   (graphView.selection.Any(s => s is ND_NodeEditor) || graphView.selection.Any(s => s is Edge));
        }

        public void AddToMenu(ContextualMenuPopulateEvent evt, ND_DrawTrelloView graphView)
        {
            evt.menu.AppendAction("Delete Animated", (action) =>
            {
                List<GraphElement> elementsToDelete = graphView.selection.OfType<GraphElement>().ToList();
                if (elementsToDelete.Count > 0)
                {
                    graphView.InitiateAnimatedRemoveElements(elementsToDelete);
                }
            }, DropdownMenuAction.AlwaysEnabled);
        }
    }
}