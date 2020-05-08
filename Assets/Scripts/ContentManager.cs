using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;


namespace SocialBeeAR
{

    [System.Serializable]
    public class NoteInfo
    {
        public float px;
        public float py;
        public float pz;
        public float qx;
        public float qy;
        public float qz;
        public float qw;
        public string note;
    }

    [System.Serializable]
    public class NotesList
    {
        // List of all notes stored in the current Place.
        public NoteInfo[] notes;
    }


    /// <summary>
    /// Class for managing virtual content
    /// </summary>
    public class ContentManager : MonoBehaviour
    {

        public List<NoteInfo> mNotesInfoList = new List<NoteInfo>();
        public List<GameObject> mNotesObjList = new List<GameObject>();

        // Prefab for the Note
        public GameObject mNotePrefab;

        private GameObject currentAnchorObj;
        private NoteInfo mCurrNoteInfo;

        [SerializeField] ARRaycastManager mRaycastManager;

        private bool enableCreatingContent = false;
        private Vector3 lastAnchorObjSpawnedPosition = Vector3.zero;
        private List<ARRaycastHit> hits = new List<ARRaycastHit>();

        private static ContentManager sInstance;
        public static ContentManager Instance
        {
            get
            {
                return sInstance;
            }
        }

        void Awake()
        {
            sInstance = this;
        }

        void Start()
        {

        }

        void Update()
        {
            //if (this.enableCreatingContent)
            //{
            //    HandleInteraction();
            //}

            if(enableDraggingCreatedAnchor)
            {
                HandleDraggingSpawnedAnchorObj();
            }
        }

        private void HandleDraggingSpawnedAnchorObj()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved && this.lastAnchorObjSpawnedPosition != Vector3.zero && currentAnchorObj != null)
                {
                    if (this.mRaycastManager != null)
                    {
                        hits.Clear();
                        if (this.mRaycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                        {
                            var hitPose = hits[0].pose;
                            Vector3 moving = hitPose.position - lastAnchorObjSpawnedPosition;
                            currentAnchorObj.transform.position += moving;

                            lastAnchorObjSpawnedPosition = hitPose.position;
                        }
                    }
                }
            }
        }

        private bool enableDraggingCreatedAnchor = false;
        public void EnableDraggingCreatedAnchor(bool enabled)
        {
            this.enableDraggingCreatedAnchor = enabled;
        }

        /// <summary>
        /// Spawn an anchor object with given 2D touch position
        /// </summary>
        /// <param name="point"></param>
        /// <param name="trackableType"></param>
        /// <returns></returns>
        private bool SpawnAnchorObj(Vector2 point, TrackableType trackableType)
        {
            hits.Clear();
            Debug.Log("point: " + point.x + " " + point.y);
            mRaycastManager.Raycast(point, hits, trackableType);

            if (hits.Count > 0)
            {
                var hitPose = hits[0].pose;
                InstantiateAnchorObj(hitPose.position);
                DebugMessageManager.Instance.PrintDebugMessage(string.Format("Anchor object created at '{0}'", hitPose.position));
                lastAnchorObjSpawnedPosition = hitPose.position;

                return true;
            }

            return false;
        }

        public void EnableCreatingContent(bool enabled)
        {
            this.enableCreatingContent = enabled;
        }

        //private void HandleInteraction()
        //{
        //    if (Input.touchCount > 0)
        //    {
        //        var touch = Input.GetTouch(0);

        //        if (touch.phase == TouchPhase.Ended)
        //        {
        //            if (EventSystem.current.currentSelectedGameObject == null)
        //            {
        //                Debug.Log("Not touching a UI button, moving on.");

        //                // Test if you are hitting an existing marker
        //                RaycastHit hit = new RaycastHit();
        //                Ray ray = Camera.main.ScreenPointToRay(touch.position);

        //                if (Physics.Raycast(ray, out hit)) //if it hits a game object
        //                {
        //                    Debug.Log("Selected an existing note.");

        //                    GameObject note = hit.transform.gameObject;

        //                    // If the previous note was deleted, switch
        //                    if (!mCurrNote)
        //                    {
        //                        mCurrNote = note;
        //                        TurnOnButtons();
        //                    }
        //                    else if (note.GetComponentInChildren<AnchorController>().mIndex != mCurrNote.GetComponentInChildren<AnchorController>().mIndex)
        //                    {
        //                        // New note selected is not the current note. Disable the buttons of the current note.
        //                        TurnOffButtons();

        //                        mCurrNote = note;

        //                        // Turn on buttons for the new selected note.
        //                        TurnOnButtons();
        //                    }
        //                    else
        //                    {
        //                        // Selected note is already the current note, just toggle buttons.
        //                        ToggleButtons();
        //                    }
        //                }
        //                else //if it doesn't hit an game object
        //                {
        //                    Debug.Log("Creating new anchor object.");

        //                    // prioritize reults types
        //                    TrackableType trackableType = TrackableType.FeaturePoint;
        //                    if (SpawnAnchorObj(touch.position, trackableType))
        //                    {
        //                        Debug.Log("Found a hit test result");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}


        private void ToggleButtons()
        {
            int index = currentAnchorObj.GetComponentInChildren<AnchorController>().mIndex;
            mCurrNoteInfo = mNotesInfoList[index];

            // Toggle the edit and delete buttons
            if (!currentAnchorObj.GetComponentInChildren<AnchorController>().mActiveButtons)
            {
                TurnOnButtons();
            }
            else
            {
                TurnOffButtons();
            }
        }

        private void TurnOnButtons()
        {
            currentAnchorObj.GetComponentInChildren<AnchorController>().mEditButton.gameObject.SetActive(true);
            currentAnchorObj.GetComponentInChildren<AnchorController>().mDeleteButton.gameObject.SetActive(true);
            currentAnchorObj.GetComponentInChildren<AnchorController>().mActiveButtons = true;

            currentAnchorObj.GetComponentInChildren<AnchorController>().EnableRotation(false);
        }

        private void TurnOffButtons()
        {
            currentAnchorObj.GetComponentInChildren<AnchorController>().mEditButton.gameObject.SetActive(false);
            currentAnchorObj.GetComponentInChildren<AnchorController>().mDeleteButton.gameObject.SetActive(false);
            currentAnchorObj.GetComponentInChildren<AnchorController>().mActiveButtons = false;

            currentAnchorObj.GetComponentInChildren<AnchorController>().EnableRotation(true);
        }

        public void InstantiateAnchorObj()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved)
                {
                    // prioritize reults types
                    TrackableType trackableType = TrackableType.PlaneWithinBounds;
                    SpawnAnchorObj(touch.position, trackableType);
                }
            }
        }

        public void InstantiateAnchorObj(Vector3 notePosition)
        {
            // Instantiate new note prefab and set transform.
            GameObject note = Instantiate(mNotePrefab);
            note.transform.position = notePosition + Const.ANCHOR_Y_OFFSET;
            note.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Turn the note to point at the camera
            Vector3 targetPosition = new Vector3(Camera.main.transform.position.x,
                                                 Camera.main.transform.position.y,
                                                 Camera.main.transform.position.z);
            //note.transform.LookAt(targetPosition);
            //note.transform.Rotate(0f, -180f, 0f);

            // don't think i need to do this
            note.SetActive(true);


            if (currentAnchorObj)
            {
                TurnOffButtons();
            }

            // Set new note as the current one.
            currentAnchorObj = note;

            mCurrNoteInfo = new NoteInfo
            {
                px = note.transform.position.x,
                py = note.transform.position.y,
                pz = note.transform.position.z,
                qx = note.transform.rotation.x,
                qy = note.transform.rotation.y,
                qz = note.transform.rotation.z,
                qw = note.transform.rotation.w,
                note = ""
            };

            // Set up the buttons on each note
            note.GetComponentInChildren<AnchorController>().mEditButton.onClick.AddListener(OnEditButtonClick);
            note.GetComponentInChildren<AnchorController>().mDeleteButton.onClick.AddListener(OnDeleteButtonClick);
            TurnOnButtons();

            //EditCurrNote();
        }

        public void EditCurrentAnchorObj()
        {
            Debug.Log("Editing selected note.");

            // Activate input field
            InputField input = currentAnchorObj.GetComponentInChildren<InputField>();
            input.interactable = true;
            input.ActivateInputField();

            input.onEndEdit.AddListener(delegate { OnNoteClosed(input); });
        }

        private void OnNoteClosed(InputField input)
        {
            Debug.Log("Editor closed.");

            // Save input text, and set input field as non interactable
            mCurrNoteInfo.note = input.text;
            input.DeactivateInputField();
            input.interactable = false;

            TurnOffButtons();

            int index = currentAnchorObj.GetComponentInChildren<AnchorController>().mIndex;
            if (index < 0)
            {
                // New note being saved!
                currentAnchorObj.GetComponentInChildren<AnchorController>().mIndex = mNotesObjList.Count;
                Debug.Log("Saving note with ID " + mNotesObjList.Count);
                mNotesInfoList.Add(mCurrNoteInfo);
                mNotesObjList.Add(currentAnchorObj);
            }
            else
            {
                // Need to re-save the object.
                mNotesObjList[index] = currentAnchorObj;
                mNotesInfoList[index] = mCurrNoteInfo;
            }

            //Start mapping
            SocialBeeARMain.Instance.OnNewMapClick();
        }

        public GameObject NoteFromInfo(NoteInfo info)
        {
            GameObject note = Instantiate(mNotePrefab);
            note.transform.position = new Vector3(info.px, info.py, info.pz);
            note.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
            note.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            note.SetActive(true);

            // add listeners to the buttons
            note.GetComponentInChildren<AnchorController>().mEditButton.onClick.AddListener(OnEditButtonClick);
            note.GetComponentInChildren<AnchorController>().mDeleteButton.onClick.AddListener(OnDeleteButtonClick);


            note.GetComponentInChildren<AnchorController>().mEditButton.gameObject.SetActive(false);
            note.GetComponentInChildren<AnchorController>().mDeleteButton.gameObject.SetActive(false);
            note.GetComponentInChildren<AnchorController>().mActiveButtons = false;

            note.GetComponentInChildren<InputField>().text = info.note;

            return note;
        }

        public void OnEditButtonClick()
        {
            Debug.Log("Edit button clicked!");
            currentAnchorObj.GetComponentInChildren<AnchorController>().EnableRotation(false);

            // Set current note to the right edit button.
            mCurrNoteInfo = mNotesInfoList[currentAnchorObj.GetComponentInChildren<AnchorController>().mIndex];
            EditCurrentAnchorObj();
        }

        public void OnDeleteButtonClick()
        {
            Debug.Log("Delete button clicked!");
            DeleteCurrentNote();
        }

        private void DeleteCurrentNote()
        {
            Debug.Log("Deleting current note!");
            int index = currentAnchorObj.GetComponentInChildren<AnchorController>().mIndex;

            if (index >= 0)
            {
                Debug.Log("Index is " + index);
                mNotesObjList.RemoveAt(index);
                mNotesInfoList.RemoveAt(index);

                // Refresh Note indices
                for (int i = 0; i < mNotesObjList.Count; ++i)
                {
                    mNotesObjList[i].GetComponentInChildren<AnchorController>().mIndex = i;
                }
            }

            Destroy(currentAnchorObj);
        }

        public void ClearNotes()
        {
            foreach (var obj in mNotesObjList)
            {
                Destroy(obj);
            }

            mNotesObjList.Clear();
            mNotesInfoList.Clear();
        }

        public JObject Notes2JSON()
        {
            NotesList notesList = new NotesList
            {
                notes = new NoteInfo[mNotesInfoList.Count]
            };

            for (int i = 0; i < mNotesInfoList.Count; ++i)
            {
                notesList.notes[i] = mNotesInfoList[i];
            }

            return JObject.FromObject(notesList);
        }

        public void LoadNotesJSON(JToken mapMetadata)
        {
            ClearNotes();

            if (mapMetadata is JObject && mapMetadata["notesList"] is JObject)
            {
                NotesList notesList = mapMetadata["notesList"].ToObject<NotesList>();

                if (notesList.notes == null)
                {
                    Debug.Log("No notes created!");
                    return;
                }

                foreach (var noteInfo in notesList.notes)
                {
                    GameObject note = NoteFromInfo(noteInfo);
                    note.GetComponentInChildren<AnchorController>().mIndex = mNotesObjList.Count;

                    mNotesObjList.Add(note);
                    mNotesInfoList.Add(noteInfo);
                }
            }
        }
    }
}