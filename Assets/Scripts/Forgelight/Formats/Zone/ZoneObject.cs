﻿using System.Collections.Generic;
using Forgelight.Attributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace Forgelight.Formats.Zone
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class ZoneObject : MonoBehaviour
    {
        [Header("Actor Settings (Global)")]
        public float renderDistance;

        [Header("Instance Settings")]
        [ReadOnly]
        public string actorDefinition;

        /// <summary>
        /// Indicates whether an object should cast shadows. We mostly turn this on (on indicates don't cast shadows, oddly) when an object is indoors (being indoors, shadows don't really matter).
        /// </summary>
        private bool dontCastShadows;
        [ExposeProperty]
        public bool DontCastShadows
        {
            get
            {
                return dontCastShadows;
            }
            set
            {
                if (renderers == null || renderers.Length == 0)
                {
                    renderers = GetComponentsInChildren<Renderer>();
                }

                foreach (Renderer renderer in renderers)
                {
                    if (value)
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
                    else
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.On;
                    }
                }

                dontCastShadows = value;
            }
        }

        /// <summary>
        /// LOD multiplier. Basically allows the designers bias the LOD distance farther or closer on a per-object basis. We generally try to avoid using it and leave it at the default 1.
        /// </summary>
        public float lodMultiplier;

        public long ID { get; set; }

        //Editor Rendering.
        private const float gracePeriod = 3.0f;
        private float target = 3.0f;

        private bool visible;

        private Renderer[] renderers;
        private List<GameObject> objectsToDestroy = new List<GameObject>();

        private ForgelightExtension forgelightExtension;

        private void OnEnable()
        {
            forgelightExtension = ForgelightExtension.Instance;
        }

        private void OnValidate()
        {
            //We call the render check function as we may have changed the render distance.
            //TODO update other actors that are also using this prefab.
            CheckVisibility();
        }

        private void OnRenderObject()
        {
            if (forgelightExtension.cameraPosChanged)
            {
                target = Time.realtimeSinceStartup + gracePeriod;
            }

            if (Time.realtimeSinceStartup >= target)
            {
                CheckVisibility();
            }

            foreach (Transform child in transform)
            {
                //Check to see if the user has accidentally moved a child object and not the parent.
                if (child.transform.localPosition != Vector3.zero)
                {
                    child.transform.localPosition = Vector3.zero;
                }
            }

            if (objectsToDestroy.Count > 0)
            {
                foreach (GameObject o in objectsToDestroy)
                {
                    DestroyImmediate(o);
                }

                objectsToDestroy.Clear();

                Resources.UnloadUnusedAssets();
            }
        }

        private void CheckVisibility()
        {
            Vector3 offset = transform.position - ForgelightExtension.Instance.LastCameraPos;

            float sqrMagnitude = offset.sqrMagnitude;
            if (sqrMagnitude <= renderDistance * renderDistance)
            {
                Show();
            }

            else
            {
                Hide();
            }

            target = float.MaxValue; //We don't need to update until we move again.
        }

        public void Hide()
        {
            if (visible)
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }

                visible = false;
            }
        }

        public void Show()
        {
            if (!visible)
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(true);
                }

                visible = true;
            }
        }

        public void DestroyObject(GameObject objToDestroy)
        {
            objectsToDestroy.Add(objToDestroy);
        }
    }
}