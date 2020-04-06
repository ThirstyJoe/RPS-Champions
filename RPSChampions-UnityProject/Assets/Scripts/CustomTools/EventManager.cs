namespace ThirstyJoe.RPSChampions
{
    using System;
    using System.Collections.Generic;

    public class EventManager : Singleton<EventManager>
    {
        private static Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();


        // Prevent non-singleton constructor use.
        protected EventManager() { }

        public static void StartListening(string eventName, Action listener)
        {
            Action thisEvent;
            if (eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                //Add more event to the existing one
                thisEvent += listener;

                //Update the Dictionary
                eventDictionary[eventName] = thisEvent;
            }
            else
            {
                //Add event to the Dictionary for the first time
                thisEvent += listener;
                eventDictionary.Add(eventName, thisEvent);
            }
        }

        public static void StopListening(string eventName, Action listener)
        {
            Action thisEvent;
            if (eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                //Remove event from the existing one
                thisEvent -= listener;

                //Update the Dictionary
                eventDictionary[eventName] = thisEvent;
            }
        }

        public static void TriggerEvent(string eventName)
        {
            Action thisEvent = null;
            if (eventDictionary.TryGetValue(eventName, out thisEvent))
            {
                thisEvent.Invoke();
            }
        }
    }
}