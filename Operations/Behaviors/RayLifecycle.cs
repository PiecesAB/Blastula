using Blastula.Collision;
using Blastula.Graphics;
using Blastula.LowLevel;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Blastula.Operations
{
    /// <summary>
    /// This behavior controls a ray's (a.k.a. solid laser's) lifecycle 
    /// as it develops from a warning, expands to full thickness, sustains full thickness, and finally decays.
    /// </summary>
    /// <remarks>
    /// - The laser only has a collider when it is full-thickness.<br/>
    /// - It assumes the laser starts on its eventual collision layer, 
    /// and with its intended full-thickness graphic, and full thickness.
    /// (The execution order should store and replace this data on the first frame's Execute, before it causes collision or rendering).
    /// </remarks>
    [GlobalClass]
    [Icon(Persistent.NODE_ICON_PATH + "/ray.png")]
    public unsafe partial class RayLifecycle : AddBehavior
    {
        /// <summary>
        /// The ID of the appearance during warning stage.
        /// </summary>
        [Export] public string warningAppearance = "LaserWarning";
        [Export] public string warningSeconds = "0.8";
        [Export] public string expandSeconds = "0.2";
        [Export] public string sustainSeconds = "1.5";
        [Export] public string decaySeconds = "0.3";

        public enum LifecycleStage
        {
            Warning, Expand, Sustain, Decay, Dead
        }

        private struct Data
        {
            public LifecycleStage lifeStage;
            public Vector4 lifeTimings; // warning, expand, sustain, decay
            public float currentTime;

            public int tempRenderID;
            public int origRenderID;

            public float prevFrameScale;
            public Vector2 origScale;

            public int origColLayer;
        }

        public static BehaviorReceipt Execute(int nodeIndex, float stepSize, void* dataPtr)
        {
            BNode* nodePtr = BNodeFunctions.masterQueue + nodeIndex;
            Data* data = (Data*)dataPtr;
            float delta = stepSize / VirtualVariables.Persistent.SIMULATED_FPS;
            bool changedState = false;
            BulletWorldTransforms.Invalidate(nodeIndex); // we're already going to invalidate anyway
            Transform2D worldTransform = BulletWorldTransforms.Get(nodeIndex);
            float r = worldTransform.Rotation;

            if (data->lifeStage == LifecycleStage.Warning)
            {
                if (data->currentTime >= data->lifeTimings[0])
                {
                    changedState = true;
                    data->currentTime = 0;
                    data->prevFrameScale = 0.25f;
                    BulletWorldTransforms.Set(nodeIndex, worldTransform.Rotated(-r).ScaledLocal(new Vector2(1f, 0.25f)).Rotated(r));
                    BulletRenderer.SetRenderID(nodeIndex, data->origRenderID);
                    data->lifeStage = LifecycleStage.Expand;
                }
            }

            if (data->lifeStage == LifecycleStage.Expand)
            {
                if (data->currentTime > 0 && data->lifeTimings[1] > 0)
                {
                    float progress = data->currentTime / data->lifeTimings[1];
                    float targetScale = Mathf.Lerp(0.25f, 1f, progress);
                    float ratio = targetScale / data->prevFrameScale;
                    BulletWorldTransforms.Set(nodeIndex, worldTransform.Rotated(-r).ScaledLocal(new Vector2(1f, ratio)).Rotated(r));
                    data->prevFrameScale = targetScale;
                }

                if (data->currentTime >= data->lifeTimings[1])
                {
                    changedState = true;
                    data->currentTime = 0;
                    float reverser = 1f / data->prevFrameScale;
                    BulletWorldTransforms.Set(nodeIndex, worldTransform.Rotated(-r).ScaledLocal(new Vector2(1f, reverser)).Rotated(r));
                    nodePtr->collisionLayer = data->origColLayer;
                    data->lifeStage = LifecycleStage.Sustain;
                }
            }

            if (data->lifeStage == LifecycleStage.Sustain)
            {
                if (data->currentTime >= data->lifeTimings[2])
                {
                    changedState = true;
                    data->currentTime = 0;
                    data->prevFrameScale = 1f;
                    nodePtr->collisionLayer = CollisionManager.NONE_BULLET_LAYER;
                    data->lifeStage = LifecycleStage.Decay;
                }
            }

            if (data->lifeStage == LifecycleStage.Decay)
            {
                if (data->lifeTimings[3] > 0)
                {
                    float progress = data->currentTime / data->lifeTimings[3];
                    float targetScale = Mathf.Lerp(1f, 0.01f, progress);
                    float ratio = targetScale / data->prevFrameScale;
                    BulletWorldTransforms.Set(nodeIndex, worldTransform.Rotated(-r).ScaledLocal(new Vector2(1f, ratio)).Rotated(r));
                    data->prevFrameScale = targetScale;
                }

                if (data->currentTime >= data->lifeTimings[3])
                {
                    changedState = true;
                    data->currentTime = 0;
                    BulletRenderer.SetRenderID(nodeIndex, -1);
                    data->lifeStage = LifecycleStage.Dead;
                }
            }

            if (!changedState && data->lifeStage != LifecycleStage.Dead)
                data->currentTime += delta;
            return new BehaviorReceipt();
        }

        private int storedRenderID;
        private bool warningAppearanceDirty = true;

        public override BehaviorOrder CreateOrder(int inStructure)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            BNode* nodePtr = BNodeFunctions.masterQueue + inStructure;
            
            dataPtr->lifeTimings = new Vector4(
                warningSeconds is null or "" ? 0f : Solve(PropertyName.warningSeconds).AsSingle(),
                expandSeconds is null or "" ? 0f : Solve(PropertyName.expandSeconds).AsSingle(),
                sustainSeconds is null or "" ? 0f : Solve(PropertyName.sustainSeconds).AsSingle(),
                decaySeconds is null or "" ? 0f : Solve(PropertyName.decaySeconds).AsSingle()
            );
            dataPtr->currentTime = 0f;
            if (warningAppearanceDirty)
            {
                storedRenderID = BulletRendererManager.GetIDFromName(warningAppearance);
                warningAppearanceDirty = false;
            }
            dataPtr->tempRenderID = storedRenderID;

            dataPtr->origScale = nodePtr->transform.Scale;
            dataPtr->origRenderID = nodePtr->bulletRenderID;
            dataPtr->origColLayer = nodePtr->collisionLayer;
            nodePtr->collisionLayer = CollisionManager.NONE_BULLET_LAYER;
            BulletRenderer.SetRenderID(inStructure, dataPtr->tempRenderID);
            dataPtr->lifeStage = LifecycleStage.Warning;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        private static BehaviorOrder CreateOrderAdd(int inStructure, Ray rayMaker)
        {
            Data* dataPtr = (Data*)Marshal.AllocHGlobal(sizeof(Data));
            BNode* nodePtr = BNodeFunctions.masterQueue + inStructure;
            dataPtr->lifeTimings = new Vector4(
                rayMaker.warningSeconds is null or "" ? 0f : rayMaker.Solve(Ray.PropertyName.warningSeconds).AsSingle(),
                rayMaker.expandSeconds is null or "" ? 0f : rayMaker.Solve(Ray.PropertyName.expandSeconds).AsSingle(),
                rayMaker.sustainSeconds is null or "" ? 0f : rayMaker.Solve(Ray.PropertyName.sustainSeconds).AsSingle(),
                rayMaker.decaySeconds is null or "" ? 0f : rayMaker.Solve(Ray.PropertyName.decaySeconds).AsSingle()
            );
            dataPtr->currentTime = 0f;
            dataPtr->tempRenderID = BulletRendererManager.GetIDFromName(rayMaker.warningAppearance);

            dataPtr->origScale = nodePtr->transform.Scale;
            dataPtr->origRenderID = nodePtr->bulletRenderID;
            dataPtr->origColLayer = nodePtr->collisionLayer;
            nodePtr->collisionLayer = CollisionManager.NONE_BULLET_LAYER;
            BulletRenderer.SetRenderID(inStructure, dataPtr->tempRenderID);
            dataPtr->lifeStage = LifecycleStage.Warning;
            return new BehaviorOrder() { data = dataPtr, dataSize = sizeof(Data), func = &Execute };
        }

        public static void Add(int inStructure, Ray rayMaker)
        {
            if (inStructure < 0 || inStructure >= BNodeFunctions.mqSize) { return; }
            BNodeFunctions.AddBehavior(inStructure, CreateOrderAdd(inStructure, rayMaker));
        }
    }
}