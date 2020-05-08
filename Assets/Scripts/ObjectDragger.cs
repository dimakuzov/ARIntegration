using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SocialBeeAR
{

    /// <summary>
    /// Enable users to drage a object on multiple detected planes.
    /// </summary>
    public class ObjectDragger : MonoBehaviour
    {
        [SerializeField] private float minDistance = 0.1f;
        [SerializeField] private float maxDistance = 10.0f;

        private float distance = 0f;
        private Vector3 offsetBetweenTouchOnObjAndObjCenter = Vector3.zero;
        //[SerializeField] private Vector3 yOffset = new Vector3(0, 0.15f, 0);

        private ARRaycastManager raycastManager;
        static List<ARRaycastHit> hits = new List<ARRaycastHit>();


        private void Start()
        {
            GameObject arSessionOriginObj = Camera.main.transform.parent.gameObject;
            this.raycastManager = arSessionOriginObj.GetComponent<ARRaycastManager>();
            if(this.raycastManager == null)
            {
                DebugMessageManager.Instance.PrintDebugMessage("Cannot find ARRaycastManager!");
            }
        }


        void Update()
        {
            if (Input.touchCount > 0)
            {
                switch (Input.GetTouch(0).phase)
                {
                    case TouchPhase.Began:
                        OnTapped(Input.GetTouch(0).position);
                        break;

                    case TouchPhase.Moved:
                        OnDragged(Input.GetTouch(0).position);
                        break;

                    case TouchPhase.Ended:
                        OnDragingEnded();
                        break;

                    default:
                        // nothing
                        break;
                }
            }
        }


        //void OnTapped_OLD(Vector2 touchPosition)
        //{
        //    distance = 0f;
        //    offsetBetweenTouchOnObjAndObjCenter = Vector3.zero;
        //    Camera mainCamera = Camera.main;
        //    if (!mainCamera) return;

        //    Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        //    RaycastHit raycastHit;
        //    if (Physics.Raycast(ray, out raycastHit, maxDistance, 1 << gameObject.layer)
        //        && raycastHit.collider.gameObject == gameObject
        //        && raycastHit.distance >= minDistance)
        //    {
        //        distance = raycastHit.distance;
        //        offsetBetweenTouchOnObjAndObjCenter = transform.position - raycastHit.point;
        //    }
        //}


        //void OnDragged_OLD(Vector2 touchPosition)
        //{
        //    Camera mainCamera = Camera.main;
        //    if (!mainCamera) return;
        //    if (offsetBetweenTouchOnObjAndObjCenter == Vector3.zero) return;
        //    if (distance <= 0f) return;

        //    if(this.raycastManager != null)
        //    {
        //        if (this.raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        //        {
        //            // Raycast hits are sorted by distance, so the first one will be the closest hit.
        //            var hitPose = hits[0].pose;
        //            transform.position = hitPose.position + offsetBetweenTouchOnObjAndObjCenter + Const.ANCHOR_Y_OFFSET;
        //        }
        //    }
        //}


        private Vector3 lastHitPos = Vector3.zero;
        void OnTapped(Vector2 touchPosition)
        {
            ////disable spawning
            //ContentManager.Instance.EnableCreatingContent(false);

            distance = 0f;
            lastHitPos = Vector3.zero;
            Camera mainCamera = Camera.main;
            if (!mainCamera) return;

            //send a raycast to hit the object
            Ray ray = mainCamera.ScreenPointToRay(touchPosition);
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, maxDistance, 1 << gameObject.layer)
                && raycastHit.collider.gameObject == gameObject
                && raycastHit.distance >= minDistance)
            {
                distance = raycastHit.distance;

                //send another raycast to hit the environment plane
                if (this.raycastManager != null)
                {
                    if (this.raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                    {
                        var hitPose = hits[0].pose;
                        lastHitPos = hitPose.position;
                    }
                }
            }
        }


        private void OnDragged(Vector2 touchPosition)
        {
            Camera mainCamera = Camera.main;
            if (!mainCamera) return;
            if (lastHitPos == Vector3.zero) return;
            if (distance <= 0f) return;

            if (this.raycastManager != null)
            {
                if (this.raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    var hitPose = hits[0].pose;
                    Vector3 moving = hitPose.position - lastHitPos;
                    transform.position += moving;

                    lastHitPos = hitPose.position;
                }
            }
        }

        private void OnDragingEnded()
        {
            ////enable spawning
            //ContentManager.Instance.EnableCreatingContent(true);
        }

    }
}


