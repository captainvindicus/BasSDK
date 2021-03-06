﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

#if FULLGAME
using Sirenix.OdinInspector;
#endif

namespace BS
{
    public class HandleDefinition : InteractableDefinition
    {
        [Range(-1, 1)]
        public float defaultGrabAxisRatio;
        public List<Orientation> allowedOrientations = new List<Orientation>();
        public HandleDefinition releaseHandle;

        public float reach = 0.5f;
        public Rigidbody customRigidBody;
        public HandleDefinition slideToUpHandle;
        public HandleDefinition slideToBottomHandle;
        public float slideToHandleOffset = 0.01f;

        public enum HandSide
        {
            None,
            Right,
            Left,
            Both,
        }

        [Serializable]
        public class Orientation
        {
            public Vector3 rotation;
            public HandSide allowedHand = HandSide.Both;
            public HandSide isDefault = HandSide.None;
            public Orientation(Vector3 rotation, HandSide allowedHand, HandSide isDefault)
            {
                this.rotation = rotation;
                this.allowedHand = allowedHand;
                this.isDefault = isDefault;
            }
        }
#if FULLGAME
        public new List<ValueDropdownItem<string>> GetAllInteractableID()
        {
            return Catalog.current.GetDropdownAllID<InteractableHandle>();
        }
#endif
        Material handLeftMat;
        Material handRightMat;
        Mesh handLeftMesh;
        Mesh handRightMesh;

        protected virtual void OnValidate()
        {
            if (allowedOrientations.Count == 0)
            {
                allowedOrientations.Add(new Orientation(Vector3.zero, HandSide.Both, HandSide.Both));
            }
            //handLeftMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/hand/handLeft.mat");
            //handRightMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/hand/handRight.mat");
            //handLeftMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/hand/LeftHand.prefab").GetComponent<MeshFilter>().sharedMesh;
            //handRightMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/hand/RightHand.prefab").GetComponent<MeshFilter>().sharedMesh;

        }

        public float GetDefaultAxisPosition()
        {
            if (axisLength == 0) return 0;
            else return defaultGrabAxisRatio * (axisLength / 2);
        }

        public virtual Vector3 GetDefaultPosition()
        {
            if (axisLength == 0) return this.transform.position;
            else
            {
                return this.transform.TransformPoint(0, GetDefaultAxisPosition(), 0);
            }
        }

        public Quaternion GetDefaultLocalRotation(Side side)
        {
            foreach (Orientation orientation in allowedOrientations)
            {
                if (side == Side.Right && (orientation.allowedHand == HandSide.Both || orientation.allowedHand == HandSide.Right) && (orientation.isDefault == HandSide.Both || orientation.isDefault == HandSide.Right))
                {
                    return Quaternion.Euler(orientation.rotation);
                }
                if (side == Side.Left && (orientation.allowedHand == HandSide.Both || orientation.allowedHand == HandSide.Left) && (orientation.isDefault == HandSide.Both || orientation.isDefault == HandSide.Left))
                {
                    return Quaternion.Euler(orientation.rotation);
                }
            }
            return Quaternion.identity;
        }

        public virtual Quaternion GetNearestLocalRotation(Transform grip, Side side)
        {
            float higherDot = -1;
            Vector3 localRotation = Vector3.zero;
            foreach (Orientation orientation in allowedOrientations)
            {
                if (orientation.allowedHand == HandSide.Both || (side == Side.Right && orientation.allowedHand == HandSide.Right) || (side == Side.Left && orientation.allowedHand == HandSide.Left))
                {
                    float dot = Vector3.Dot(grip.forward, (this.transform.rotation * Quaternion.Euler(orientation.rotation)) * Vector3.forward) + Vector3.Dot(grip.up, (this.transform.rotation * Quaternion.Euler(orientation.rotation)) * Vector3.up);
                    if (dot > higherDot)
                    {
                        higherDot = dot;
                        localRotation = orientation.rotation;
                    }
                }
            }
            return Quaternion.Euler(localRotation);
        }

        public virtual float GetNearestAxisPosition(Vector3 position)
        {
            Vector3 referenceLocalPos = this.transform.InverseTransformPoint(position);
            return Mathf.Clamp(referenceLocalPos.y, -(axisLength / 2), (axisLength / 2));
        }

        public virtual Vector3 GetNearestPositionAlongAxis(Vector3 position)
        {
            return this.transform.TransformPoint(new Vector3(0, GetNearestAxisPosition(position), 0));
        }

        public bool IsAllowed(Side side)
        {
            foreach (Orientation orientation in allowedOrientations)
            {
                if (side == Side.Right && (orientation.allowedHand == HandSide.Both || orientation.allowedHand == HandSide.Right))
                {
                    return true;
                }
                if (side == Side.Left && (orientation.allowedHand == HandSide.Both || orientation.allowedHand == HandSide.Left))
                {
                    return true;
                }
            }
            return false;
        }

        [ContextMenu("Calculate Reach")]
        public void CalculateReach()
        {
            float farthestDamagerDist = 0;
            foreach (ItemDefinition.ColliderGroup colliderGroup in this.GetComponentInParent<ItemDefinition>().colliderGroups)
            {
                foreach (Collider collider in colliderGroup.colliders)
                {
                    Vector3 farthestPoint = collider.ClosestPointOnBounds(this.transform.position + (this.transform.up.normalized * 10));
                    float dist = this.transform.InverseTransformPoint(farthestPoint).y;
                    if (dist > farthestDamagerDist)
                    {
                        farthestDamagerDist = dist;
                    }
                }
            }
            reach = farthestDamagerDist - GetDefaultAxisPosition();
        }
        
        protected override void OnDrawGizmosSelected()
        {
             Matrix4x4[] posMatrix = new Matrix4x4[1];
            posMatrix[0] = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

            Gizmos.color = Common.HueColourValue(HueColorNames.Yellow);
            Gizmos.DrawWireSphere(this.transform.position, reach);
            base.OnDrawGizmosSelected();
            foreach (Orientation orientation in allowedOrientations)
            {
                for (int i = 0; i < 2; i++)
                {
                    float displace = 0.1f;
                    float ind = allowedOrientations.IndexOf(orientation);
                    if (i == 1 && ind == 0)
                    {
                        displace = 0;
                    }
                    if (orientation.allowedHand != HandSide.None && (i == 0 || displace == 0))
                    {
                        Gizmos.matrix = Matrix4x4.TRS(this.transform.TransformPoint(displace, GetDefaultAxisPosition(), ind / 15), this.transform.rotation * Quaternion.Euler(orientation.rotation), Vector3.one);

                        if (orientation.allowedHand == HandSide.Both)
                        {
                            Common.DrawGizmoArrow(Vector3.zero, new Vector3(0.02f, 0, 0), Common.HueColourValue(HueColorNames.Green), 0.01f);
                            Common.DrawGizmoArrow(Vector3.zero, new Vector3(-0.02f, 0, 0), Common.HueColourValue(HueColorNames.Red), 0.01f);
                            Gizmos.color = Common.HueColourValue(HueColorNames.Purple);
                        }
                        else if (orientation.allowedHand == HandSide.Right)
                        {
                            Common.DrawGizmoArrow(Vector3.zero, new Vector3(0.02f, 0, 0), Common.HueColourValue(HueColorNames.Green), 0.01f);
                            Gizmos.color = Common.HueColourValue(HueColorNames.Green);
                        }
                        else if (orientation.allowedHand == HandSide.Left)
                        {
                            Common.DrawGizmoArrow(Vector3.zero, new Vector3(-0.02f, 0, 0), Common.HueColourValue(HueColorNames.Red), 0.01f);
                            Gizmos.color = Common.HueColourValue(HueColorNames.Red);
                        }

                        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.01f, 0.05f, 0.01f));
                        Gizmos.DrawWireCube(new Vector3(0, 0.03f, 0.01f), new Vector3(0.01f, 0.01f, 0.03f));
                        if (orientation.isDefault != HandSide.None)
                        {
                            if (orientation.isDefault == HandSide.Both)
                            {
                                Gizmos.color = Common.HueColourValue(HueColorNames.Purple);
                            }
                            else if (orientation.isDefault == HandSide.Right)
                            {
                                Gizmos.color = Common.HueColourValue(HueColorNames.Green);
                            }
                            else if (orientation.isDefault == HandSide.Left)
                            {
                                Gizmos.color = Common.HueColourValue(HueColorNames.Red);
                            }

                            Gizmos.DrawWireSphere(new Vector3(0, 0.03f, 0.025f), 0.005f);
                        }
                    }
                }
            }
        }
    }
}
