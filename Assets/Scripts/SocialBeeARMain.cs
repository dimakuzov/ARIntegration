using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.XR.ARFoundation;


namespace SocialBeeAR
{

    public class SocialBeeARMain : MonoBehaviour, PlacenoteListener
    {
        [SerializeField] GameObject mMapSelectedPanel;

        [SerializeField] GameObject mInitPanel;
        [SerializeField] GameObject createButtons;
        [SerializeField] GameObject newExperienceButton;
        [SerializeField] GameObject cancelNewExperienceButton;

        [SerializeField] GameObject mMappingPanel;

        [SerializeField] GameObject mMapListPanel;

        [SerializeField] GameObject mMapLoadPanel;

        [SerializeField] GameObject mMapLoadButtonPanel;

        [SerializeField] RawImage mLocalizationThumbnail;

        [SerializeField] GameObject mListElement;
        [SerializeField] RectTransform mListContentParent;
        [SerializeField] ToggleGroup mToggleGroup;
        [SerializeField] Text mLabelText;

        [SerializeField] Image saveButtonProgressBar;

        
        private bool mapQualityThresholdCrossed = false;
        private float minMapSize = 200;

        private bool mPlacenoteInit = false;
        private bool mLoadedOnce = false;

        private LibPlacenote.MapMetadataSettable mCurrMapDetails;
        private LibPlacenote.MapInfo mSelectedMapInfo;

        private string mSelectedMapId
        {
            get
            {
                return mSelectedMapInfo != null ? mSelectedMapInfo.placeId : null;
            }
        }

        private string mSaveMapId = null;

        private static SocialBeeARMain sInstance;
        public static SocialBeeARMain Instance
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
            // wait for ar session to start tracking and for placenote to initialize
            StartCoroutine(WaitForARSessionThenStart());
        }


        IEnumerator WaitForARSessionThenStart()
        {
            mLabelText.text = "Initializing Placenote AR Session...";
            while (ARSession.state != ARSessionState.SessionTracking || !LibPlacenote.Instance.Initialized())
            {
                yield return null;
            }

            LibPlacenote.Instance.RegisterListener(this); // Register listener for onStatusChange and OnPose
            FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);

            // AR Session has started tracking here. Now start the session
            Input.location.Start();

            mMapListPanel.SetActive(false);

            // Localization thumbnail handler.
            mLocalizationThumbnail.gameObject.SetActive(false);

            // Set up the localization thumbnail texture event.
            LocalizationThumbnailSelector.Instance.TextureEvent += (thumbnailTexture) =>
            {
                if (mLocalizationThumbnail == null)
                {
                    return;
                }

                RectTransform rectTransform = mLocalizationThumbnail.rectTransform;
                if (thumbnailTexture.width != (int)rectTransform.rect.width)
                {
                    rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal, thumbnailTexture.width * 2);
                    rectTransform.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical, thumbnailTexture.height * 2);
                    rectTransform.ForceUpdateRectTransforms();
                }
                mLocalizationThumbnail.texture = thumbnailTexture;
            };

            mLabelText.text = "Initialization compelted, GO!";
        }


        // Update is called once per frame
        void Update()
        {
            if (LibPlacenote.Instance.Initialized())
            {
                //Check if allow users to add/edit content
                CheckEnableContentPlacing();

                //Print some info for debugging
                LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap();
                if (map != null)
                    DebugMessageManager.Instance.UpdateMapSize(map.Length);

                if (LibPlacenote.Instance.GetMode() == LibPlacenote.MappingMode.MAPPING)
                    DebugMessageManager.Instance.UpdateMappingQuality(LibPlacenote.Instance.GetMappingQuality().ToString());
            }
        }


        public void OnSearchMapsClick()
        {

            mLabelText.text = "Searching for saved maps";

            LocationInfo locationInfo = Input.location.lastData;

            //LibPlacenote.Instance.SearchMaps(locationInfo.latitude, locationInfo.longitude, radiusSearch, (mapList) =>
            LibPlacenote.Instance.SearchMaps("Note:", (mapList) =>
            {
                foreach (Transform t in mListContentParent.transform)
                {
                    Destroy(t.gameObject);
                }

                DebugMessageManager.Instance.PrintDebugMessage(string.Format("Found {0} maps.", mapList.Length));
                if (mapList.Length == 0)
                {
                    mLabelText.text = "No maps found. Create a map first!";
                    return;
                }

                // Render the map list!
                foreach (LibPlacenote.MapInfo mapId in mapList)
                {
                    if (mapId.metadata.userdata != null)
                    {
                        Debug.Log(mapId.metadata.userdata.ToString(Formatting.None));
                    }

                    AddMapToList(mapId);
                }

                mLabelText.text = "Please select a map to load";
                mMapListPanel.SetActive(true);
                mInitPanel.SetActive(false);

            });
        }
        

        // To get a list of all maps you have created, use list maps
        /*
        public void OnListMapClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            foreach (Transform t in mListContentParent.transform)
            {
                Destroy(t.gameObject);
            }

            mMapListPanel.SetActive(true);
            mInitPanel.SetActive(false);
            mRadiusSlider.gameObject.SetActive(true);

            LibPlacenote.Instance.ListMaps((mapList) =>
            {
            // Render the map list!
            foreach (LibPlacenote.MapInfo mapInfoItem in mapList)
                {
                    if (mapInfoItem.metadata.userdata != null)
                    {
                        Debug.Log(mapInfoItem.metadata.userdata.ToString(Formatting.None));
                    }

                    AddMapToList(mapInfoItem);
                }
            });
        }
        */

        public void OnCancelSelectingMapClick()
        {
            mMapSelectedPanel.SetActive(false);
            mMapListPanel.SetActive(false);
            mInitPanel.SetActive(true);
        }

        public void OnExitClick()
        {
            mLabelText.text = "Session was reset. You can start new map or load your map again.";

            mInitPanel.SetActive(true);
            mMapLoadPanel.SetActive(false);
            mMapLoadButtonPanel.SetActive(false);
            mLocalizationThumbnail.gameObject.SetActive(false);
            
            mLoadedOnce = false;

            LibPlacenote.Instance.StopSession();
            FeaturesVisualizer.ClearPointcloud();

            GetComponent<ContentManager>().ClearNotes();
        }

        void AddMapToList(LibPlacenote.MapInfo mapInfo)
        {
            GameObject newElement = Instantiate(mListElement) as GameObject;
            if(newElement != null)
            {
                MapInfoElement listElement = newElement.GetComponent<MapInfoElement>();
                listElement.Initialize(mapInfo, mToggleGroup, mListContentParent, (value) =>
                {
                    OnMapSelected(mapInfo);
                });
            }
        }

        void OnMapSelected(LibPlacenote.MapInfo mapInfo)
        {
            mSelectedMapInfo = mapInfo;
            mMapSelectedPanel.SetActive(true);
        }

        //public void OnLoadMapClick_OLD()
        //{
        //    if (!LibPlacenote.Instance.Initialized())
        //    {
        //        Debug.Log("SDK not yet initialized");
        //        return;
        //    }

        //    mLabelText.text = "Loading Map ID: " + mSelectedMapId;

        //    LibPlacenote.Instance.LoadMap(mSelectedMapId, (completed, faulted, percentage) =>
        //    {
        //        if (completed)
        //        {
        //            mMapSelectedPanel.SetActive(false);
        //            mMapListPanel.SetActive(false);
        //            mInitPanel.SetActive(false);

        //            mMapLoadPanel.SetActive(true);
        //            mMapLoadButtonPanel.SetActive(false);
        //            mLocalizationThumbnail.gameObject.SetActive(true);

        //          //Commented off by Cliff Lin/////////////
        //          //FeaturesVisualizer.DisablePointcloud();

        //            LibPlacenote.Instance.StartSession();
        //            DebugMessageManager.Instance.PrintDebugMessage("Map loading completed!");

        //            //mLabelText.text = "Loaded Map. Trying to localize";

        //        }
        //        else if (faulted)
        //        {
        //            mLabelText.text = "Failed to load ID: " + mSelectedMapId;
        //        }
        //        else
        //        {
        //            mLabelText.text = "Map Download: " + percentage.ToString("F2") + "/1.0";
        //        }
        //    });
        //}

        public void OnLoadMapClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            mLabelText.text = "Loading Map ID: " + mSelectedMapId;
            DebugMessageManager.Instance.PrintDebugMessage("Loading Map: " + mSelectedMapId);

            LibPlacenote.Instance.LoadMap(mSelectedMapId, (completed, faulted, percentage) =>
            {
                if (completed)
                {
                    mMapSelectedPanel.SetActive(false);
                    mMapListPanel.SetActive(false);
                    mInitPanel.SetActive(false);

                    mMapLoadPanel.SetActive(true);
                    mMapLoadButtonPanel.SetActive(false);
                    mLocalizationThumbnail.gameObject.SetActive(true);

                    //Start localization session
                    LibPlacenote.Instance.StartSession();
                    DebugMessageManager.Instance.PrintDebugMessage("Session started in Locating mode");
                }
                else if (faulted)
                {
                    mLabelText.text = "Failed to load ID: " + mSelectedMapId;
                    DebugMessageManager.Instance.PrintDebugMessage("Failed to load ID: " + mSelectedMapId);
                }
                else
                {
                    mLabelText.text = "Map Download: " + percentage.ToString("F2") + "/1.0";
                }
            });
        }

        private void CheckEnableContentPlacing()
        {
            ContentManager.Instance.EnableCreatingContent(LibPlacenote.Instance.IsPerformingMapping());
        }

        public void OnExtendingMap()
        {
            mMapSelectedPanel.SetActive(false);
            mMapListPanel.SetActive(false);
            mInitPanel.SetActive(false);

            mMapLoadPanel.SetActive(false);
            mMapLoadButtonPanel.SetActive(false);
            mLocalizationThumbnail.gameObject.SetActive(false);

            mMappingPanel.SetActive(true);

            //LibPlacenote.Instance.StopSession(); //NO NEED!
            //DebugMessageManager.Instance.PrintDebugMessage("Session stopped");

            FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);

            LibPlacenote.Instance.StartSession(true);
            DebugMessageManager.Instance.PrintDebugMessage("Session started in extending mode");
        }

        public void OnPauseMapping()
        {
            LibPlacenote.Instance.StopSendingFrames();
            this.mMappingPanel.transform.GetChild(2).gameObject.SetActive(false);
            this.mMappingPanel.transform.GetChild(3).gameObject.SetActive(true);
        }

        public void OnResumeMapping()
        {
            LibPlacenote.Instance.RestartSendingFrames();
            this.mMappingPanel.transform.GetChild(2).gameObject.SetActive(true);
            this.mMappingPanel.transform.GetChild(3).gameObject.SetActive(false);
        }

        public void OnDeleteMapClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            mLabelText.text = "Deleting Map ID: " + mSelectedMapId;
            LibPlacenote.Instance.DeleteMap(mSelectedMapId, (deleted, errMsg) =>
            {
                if (deleted)
                {
                    mMapSelectedPanel.SetActive(false);
                    mLabelText.text = "Deleted ID: " + mSelectedMapId;
                    OnSearchMapsClick();
                }
                else
                {
                    mLabelText.text = "Failed to delete ID: " + mSelectedMapId;
                }
            });
        }

        public void OnNewExperienceClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            mLabelText.text = "Drage an experience to the view.";
            createButtons.SetActive(true);
            newExperienceButton.SetActive(false);
            cancelNewExperienceButton.SetActive(true);
        }

        public void OnCancelNewExperienceClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            mLabelText.text = "";
            createButtons.SetActive(false);
            newExperienceButton.SetActive(true);
            cancelNewExperienceButton.SetActive(false);
        }

        public void OnNewMapClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            mLabelText.text = "Slowly move your phone and scan the surrounding, make sure to collect enough purple crosses, until the save button is ready.";

            mInitPanel.SetActive(false);
            mMappingPanel.SetActive(true);

            saveButtonProgressBar.gameObject.GetComponent<Image>().fillAmount = 0;
            mapQualityThresholdCrossed = false;

            // Enable pointcloud
            FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);

            LibPlacenote.Instance.StartSession();
            DebugMessageManager.Instance.PrintDebugMessage("Session started in Mapping mode");
        }

        public void OnCancelMappingClick()
        {
            LibPlacenote.Instance.StopSession();

            mMappingPanel.SetActive(false);
            mInitPanel.SetActive(true);

            DebugMessageManager.Instance.UpdateMapSize(0);
        }


        public void OnUpdateNotesClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            mLabelText.text = "Updating note data...";

            LibPlacenote.MapMetadataSettable metadataUpdated = new LibPlacenote.MapMetadataSettable();

            metadataUpdated.name = mSelectedMapInfo.metadata.name;

            JObject userdata = new JObject();
            metadataUpdated.userdata = userdata;

            JObject notesList = GetComponent<ContentManager>().Notes2JSON();
            userdata["notesList"] = notesList;
            metadataUpdated.location = mSelectedMapInfo.metadata.location;

            LibPlacenote.Instance.SetMetadata(mSelectedMapId, metadataUpdated, (success) =>
            {
                if (success)
                {
                    mLabelText.text = "Note updated! To end the session, click Exit.";

                    Debug.Log("Meta data successfully updated!");
                }
                else
                {
                    Debug.Log("Meta data failed to save");
                }
            });

        }


        public void OnSaveMapClick()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                Debug.Log("SDK not yet initialized");
                return;
            }

            if (!mapQualityThresholdCrossed)
            {
                mLabelText.text = "Map quality is not good enough to save. Scan a small area with many features and try again.";
                return;
            }

            bool useLocation = Input.location.status == LocationServiceStatus.Running;
            LocationInfo locationInfo = Input.location.lastData;

            mLabelText.text = "Saving...";

            LibPlacenote.Instance.SaveMap((mapId) =>
            {
                LibPlacenote.Instance.StopSession();
                FeaturesVisualizer.ClearPointcloud();

                mSaveMapId = mapId;

                mMappingPanel.SetActive(false);

                LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();


                metadata.name = "Note: " + System.DateTime.Now.ToString();

                mLabelText.text = "Saved Map Name: " + metadata.name;

                JObject userdata = new JObject();
                metadata.userdata = userdata;

                JObject notesList = GetComponent<ContentManager>().Notes2JSON();

                userdata["notesList"] = notesList;
                GetComponent<ContentManager>().ClearNotes();

                if (useLocation)
                {
                    metadata.location = new LibPlacenote.MapLocation();
                    metadata.location.latitude = locationInfo.latitude;
                    metadata.location.longitude = locationInfo.longitude;
                    metadata.location.altitude = locationInfo.altitude;
                }

                LibPlacenote.Instance.SetMetadata(mapId, metadata, (success) =>
                {
                    if (success)
                    {
                        Debug.Log("Meta data successfully saved!");
                    }
                    else
                    {
                        Debug.Log("Meta data failed to save");
                    }
                });
                mCurrMapDetails = metadata;
            }, (completed, faulted, percentage) =>
            {
                if (completed)
                {
                    mLabelText.text = "Upload Complete! You can now click My Maps and choose a map to load.";
                    mInitPanel.SetActive(true);

                }
                else if (faulted)
                {
                    mLabelText.text = "Upload of Map Named: " + mCurrMapDetails.name + "faulted";
                }
                else
                {
                    mLabelText.text = "Uploading Map " + "(" + (percentage * 100.0f).ToString("F2") + " %)";
                }
            });
        }


        public void OnLocalized()
        {
            DebugMessageManager.Instance.PrintDebugMessage("OnLocalized, loading content...");

            mLabelText.text = "Localized. Add or edit notes and click Update. Or click Exit to end the session.";
            GetComponent<ContentManager>().LoadNotesJSON(mSelectedMapInfo.metadata.userdata);

            DebugMessageManager.Instance.PrintDebugMessage("Content loaded.");

            mLocalizationThumbnail.gameObject.SetActive(false);
            mMapLoadButtonPanel.SetActive(true);
        }


        public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
        {
            if(LibPlacenote.Instance.IsPerformingMapping())
            {
                CheckIfEnoughPointcloudCollectedForMap();
            }   
        }


        private void CheckIfEnoughPointcloudCollectedForMap()
        {
            // get the full point built so far
            List<Vector3> fullPointCloudMap = FeaturesVisualizer.GetPointCloud();

            // Check if either are null
            if (fullPointCloudMap == null)
            {
                Debug.Log("Point cloud is null");
                return;
            }
            Debug.Log("Point cloud size = " + fullPointCloudMap.Count);

            saveButtonProgressBar.gameObject.GetComponent<Image>().fillAmount = fullPointCloudMap.Count / minMapSize;

            if (fullPointCloudMap.Count >= minMapSize)
            {
                if (LibPlacenote.Instance.GetMode() == LibPlacenote.MappingMode.MAPPING)
                    mLabelText.text = "Reeady to save the map.";

                // Check the map quality to confirm whether you can save
                if (LibPlacenote.Instance.GetMappingQuality() == LibPlacenote.MappingQuality.GOOD)
                {
                    mapQualityThresholdCrossed = true;
                }
            }
        }


        public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
        {
            string currentMode = LibPlacenote.Instance.GetMode().ToString();
            string status = currStatus.ToString();

            DebugMessageManager.Instance.UpdateStatus(currentMode, status);
            Debug.Log(string.Format("Mode: '{0}', Status changed: '{1}'->'{2}'", currentMode, prevStatus.ToString(), status));

            if (currStatus == LibPlacenote.MappingStatus.LOST && prevStatus == LibPlacenote.MappingStatus.WAITING)
            {
                mLabelText.text = "Point your phone at the area shown in the thumbnail";
            }

            DebugMessageManager.Instance.EnableMappingQualityInfo(LibPlacenote.Instance.IsPerformingMapping());
        }

    }

}