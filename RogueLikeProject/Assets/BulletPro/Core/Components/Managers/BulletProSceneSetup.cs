using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    public class BulletProSceneSetup : MonoBehaviour
    {
        public static BulletProSceneSetup instance;

        public bool makePersistentBetweenScenes = false;

        Transform mainTransform;
        Vector3 cachedRight, cachedUp, cachedForward;

		public Vector3 right => enabled ? cachedRight : ((mainTransform == null) ? Vector3.right : mainTransform.right);
		public Vector3 up => enabled ? cachedUp : ((mainTransform == null) ? Vector3.up : mainTransform.up);
		public Vector3 forward => enabled ? cachedForward : ((mainTransform == null) ? Vector3.forward : mainTransform.forward);

        void Awake()
        {
            if (instance == null) instance = this;
			else
            {
                Debug.LogWarning("BulletPro Warning: there is more than one instance of BulletSceneSetup in the scene!");
                Destroy(gameObject);
                return;
            }

            mainTransform = transform;

            if (makePersistentBetweenScenes)
                DontDestroyOnLoad(gameObject);

            UpdateVectorCache();
        }

        // An almost-empty script.
        // Its inspector displays a help message and shows gameplay plane orientation.

        #region main transform (bullet canvas) handling

        public Transform GetBulletCanvas() => mainTransform;

        // Caches local directions so matrix calculations aren't done once per bullet every frame
        void UpdateVectorCache()
		{
            if (mainTransform == null) return;
			cachedRight = mainTransform.right;
			cachedUp = mainTransform.up;
			cachedForward = mainTransform.forward;
		}

        void Update()
        {
            UpdateVectorCache();
        }

        #endregion

        public bool enableGizmo = true;
        public Color gizmoColor = Color.white;

        void OnDrawGizmos()
        {
            if (!enableGizmo) return;

            Matrix4x4 oldmat = Gizmos.matrix;

            float gizmoSize = 2f;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Vector3 cubeSize = new Vector3(gizmoSize, gizmoSize, gizmoSize*0.05f);
            Gizmos.DrawCube(transform.position + cubeSize * 0.5f, cubeSize);

            Gizmos.matrix = oldmat;
        }

        // buildNumber indicates in what version this scene setup has been created.
        // Can be used by future updates that would need to change Scene Setup structure.
        // Please don't modify this!
        [HideInInspector] [SerializeField] private int buildNumber;
        public int GetBuildNumber() => buildNumber;
        public void SetBuildNumber(int newValue)
        {
            buildNumber = newValue;
        }
    }
}
