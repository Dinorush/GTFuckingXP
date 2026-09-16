using AIGraph;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace XpExpansions.Util
{
    public static class DoorUtil
    {
        private const float PortalCheckBuffer = 0.25f;
        private const float MinDotInNode = 0.1f;
        private static readonly List<AIG_CoursePortal> _portalCache = new();

        public static bool TryGetNearbyPortal(Vector3 pos, AIG_CourseNode node, [MaybeNullWhen(false)] out AIG_CoursePortal portal)
        {
            float bestDist = float.MaxValue;
            portal = null;
            foreach (var nodePortal in node.m_portals)
            {
                var diff = pos - nodePortal.Position;
                float distSqr = diff.sqrMagnitude;

                // Distance cap to prevent false positives when the door juts out (e.g. a hallway that extends partially into another room)
                nodePortal.m_cullPortal.GetShapeWidthHeight(out var width, out var height);
                width /= 2;
                height /= 2;
                width += PortalCheckBuffer;
                height += PortalCheckBuffer;
                if (distSqr < bestDist && distSqr < width * width + height * height)
                {
                    portal = nodePortal;
                    bestDist = distSqr;
                }
            }

            return portal != null;
        }

        public static bool TryGetCrossPortal(Vector3 pos, AIG_CourseNode nodeA, AIG_CourseNode nodeB, [MaybeNullWhen(false)] out AIG_CoursePortal portal)
        {
            var nodeBID = nodeB.NodeID;
            foreach (var nodePortal in nodeA.m_portals)
            {
                if (nodePortal.GetOppositeNode(nodeA).NodeID == nodeBID)
                    _portalCache.Add(nodePortal);
            }

            if (_portalCache.Count == 0)
            {
                portal = null;
                return false;
            }

            if (_portalCache.Count == 1)
            {
                portal = _portalCache[0];
                _portalCache.Clear();
                return true;
            }

            float bestDist = float.MaxValue;
            portal = null!;
            foreach (var nodePortal in _portalCache)
            {
                var diff = pos - nodePortal.Position;
                float distSqr = diff.sqrMagnitude;
                if (distSqr < bestDist)
                {
                    portal = nodePortal;
                    bestDist = distSqr;
                }
            }
            _portalCache.Clear();
            return true;
        }

        public static bool IsOnSameSide(Vector3 pos, AIG_CourseNode checkNode, AIG_CoursePortal portal)
        {
            var cPortal = portal.m_cullPortal;
            var diffToPos = pos - cPortal.m_center;
            var dirIntoNode = cPortal.GetDirIntoNode(checkNode.m_cullNode);
            return Vector3.Dot(dirIntoNode, diffToPos) > MinDotInNode;
        }

        public static AIG_CourseNode GetCorrectNode(Vector3 pos, AIG_CourseNode node)
        {
            if (TryGetNearbyPortal(pos, node, out var portal))
            {
                var checkNode = portal.GetOppositeNode(node);
                if (IsOnSameSide(pos, checkNode, portal))
                    return checkNode;
            }
            return node;
        }

        public static bool CanTouchNode(Vector3 pos, AIG_CourseNode node, AIG_CourseNode checkNode)
        {
            if (node.NodeID == checkNode.NodeID) return true;

            if (!TryGetCrossPortal(pos, node, checkNode, out var portal)) return false;

            if (portal.IsTraversable) return true;

            return IsOnSameSide(pos, checkNode, portal);
        }

        public static bool CanTouchNode(Vector3 pos, AIG_CourseNode checkNode)
        {
            if (!TryGetNearbyPortal(pos, checkNode, out var portal) || portal.IsTraversable) return true;

            return IsOnSameSide(pos, checkNode, portal);
        }
    }
}
