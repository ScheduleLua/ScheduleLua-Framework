using MoonSharp.Interpreter;
using ScheduleLua.API.Base;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScheduleLua.API.Core.TypeProxies
{
    /// <summary>
    /// Proxy class for RaycastHit to avoid exposing struct directly
    /// </summary>
    [MoonSharpUserData]
    public class RaycastHitProxy : UnityTypeProxyBase<RaycastHit>
    {
        private RaycastHit _hit;

        public RaycastHitProxy()
        {
            // Default constructor needed for AOT compatibility
        }

        public RaycastHitProxy(RaycastHit hit)
        {
            _hit = hit;
        }

        public Vector3Proxy point => new Vector3Proxy(_hit.point);
        public Vector3Proxy normal => new Vector3Proxy(_hit.normal);
        public float distance => _hit.distance;
        public string colliderName => _hit.collider?.name ?? "none";
        public string gameObjectName => _hit.transform?.gameObject?.name ?? "none";

        public static implicit operator RaycastHit(RaycastHitProxy proxy) => proxy._hit;
        public static implicit operator RaycastHitProxy(RaycastHit hit) => new RaycastHitProxy(hit);

        public override string ToString() => $"Hit: {gameObjectName} at {point}";
    }
}
