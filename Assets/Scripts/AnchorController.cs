using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    public class AnchorController : MonoBehaviour
    {
        // This is set to -1 when instantiated, and assigned when saving notes.
        [SerializeField] public int mIndex = -1;
        [SerializeField] public bool mActiveButtons = false;
        [SerializeField] public Text positionText;

        public Button mEditButton;
        public Button mDeleteButton;
        private bool enableRotation = true;

        private void Update()
        {
            if (positionText != null)
            {
                positionText.text = transform.position.ToString();
            }

            if(enableRotation)
            {
                transform.Rotate(Vector3.up, Time.deltaTime * 15);
            }
        }

        public void EnableRotation(bool enabled)
        {
            this.enableRotation = enabled;
        }

    }
}
