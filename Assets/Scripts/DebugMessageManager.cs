using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace SocialBeeAR
{

    public class DebugMessageManager : MonoBehaviour
    {
        [SerializeField] private GameObject debugPanel;
        private DebugPanelController debugPanelController;

        [SerializeField] private Text infoMapSizeText;

        [SerializeField] private GameObject infoMappingQualityTextParent;
        private Text infoMappingQualityText;

        [SerializeField] private GameObject infoStatusParent;
        private Text infoMode;
        private Text infoStatus;

        private static DebugMessageManager _instance;
        public static DebugMessageManager Instance
        {
            get
            {
                return _instance;
            }
        }


        public void Awake()
        {
            _instance = this;
        }


        public void Start()
        {
            if (debugPanel != null)
                debugPanelController = debugPanel.GetComponent<DebugPanelController>();

            if (infoMappingQualityTextParent != null)
                infoMappingQualityText = infoMappingQualityTextParent.transform.GetChild(1).GetComponent<Text>();

            if(infoStatusParent != null)
            {
                infoMode = infoStatusParent.transform.GetChild(1).GetComponent<Text>();
                infoStatus = infoStatusParent.transform.GetChild(3).GetComponent<Text>();
            }
        }


        public void UpdateMapSize(int mapLength)
        {
            if (this.infoMapSizeText != null)
            {
                this.infoMapSizeText.text = mapLength.ToString();
            }
        }


        public void UpdateMappingQuality(string mappingQuality)
        {
            if (this.infoMappingQualityText != null)
            {
                this.infoMappingQualityText.text = mappingQuality;
            }
        }


        public void EnableMappingQualityInfo(bool enabled)
        {
            if(infoMappingQualityTextParent != null)
            {
                infoMappingQualityTextParent.SetActive(enabled);
            }
        }


        public void UpdateStatus(string mode, string status)
        {
            if (infoMode != null)
                this.infoMode.text = mode;

            if (infoStatus != null)
                this.infoStatus.text = status;
        }


        public void PrintDebugMessage(string message)
        {
            if (debugPanelController != null)
            {
                //debugPanelController.PushMessage(message);

                MainThreadTaskQueue.InvokeOnMainThread(() =>
                {
                    debugPanelController.PushMessage(message);
                });
            }
        }

    }

}


