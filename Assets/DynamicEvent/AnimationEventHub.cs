using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimationEventHub : MonoBehaviour
{
    public string eventToAction;

    [SerializeField]
    private List<DynamicEvent> dynamicEvents = new List<DynamicEvent>();

    //Function to Invoke function in dynamic Events

    public void TriggerEvent(string eventName)
    {
        // Find the event in our list that matches the provided name.
        DynamicEvent eventToTrigger = dynamicEvents.FirstOrDefault(e => e.eventName == eventName);

        if (eventToTrigger != null)
        {
            // If we found a matching event, tell it to invoke its target method.
            eventToTrigger.Invoke();
        }
        else
        {
            // If no event with that name is found, log a warning for easier debugging.
            Debug.LogWarning($"AnimationEventHub: Event with name '{eventName}' not found on {gameObject.name}.", this);
        }
    }

    public void OnValidate()
    {
        TriggerEvent(eventToAction);
    }
}
