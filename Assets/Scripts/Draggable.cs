using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SocialBeeAR
{
    public class Draggable : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private bool startDragging = false;
        private int startDraggingCounter = 0;
        private int holdThreshold = 10;

        public void OnPointerDown(PointerEventData eventData)
        {
            startDraggingCounter = 0;
            startDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(++startDraggingCounter == holdThreshold && startDragging == true)
            {
                startDragging = false;

                //triggers content creating mode
                ContentManager.Instance.InstantiateAnchorObj();
                ContentManager.Instance.EnableDraggingCreatedAnchor(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            startDragging = false;
            ContentManager.Instance.EnableDraggingCreatedAnchor(false);
            ContentManager.Instance.EditCurrentAnchorObj();
        }
    }

}




