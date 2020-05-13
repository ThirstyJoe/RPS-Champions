// copied script from: https://www.gamasutra.com/blogs/DylanWolf/20190128/335228/Stupid_Unity_UI_Navigation_Tricks.php
namespace ThirstyJoe.RPSChampions
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class PreventDeselectionGroup : MonoBehaviour
    {
        EventSystem evt;

        private void Start()
        {
            evt = EventSystem.current;

        }

        GameObject sel;

        private void Update()
        {
            if (evt.currentSelectedGameObject != null && evt.currentSelectedGameObject != sel)
                sel = evt.currentSelectedGameObject;
            else if (sel != null &&
                    evt.currentSelectedGameObject == null)
                evt.SetSelectedGameObject(sel);
        }
    }
}