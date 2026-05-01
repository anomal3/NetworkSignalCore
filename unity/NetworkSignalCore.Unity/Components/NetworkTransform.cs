// Synchronizes transform (position, rotation, scale) over the network.
// Attach to any GameObject alongside NetworkBehaviour.
// Server sends positions; client interpolates between received states.
using System;
using System.Threading.Tasks;
using NetworkSignalCore.Core.Attributes;
using UnityEngine;

namespace NetworkSignalCore.Unity.Components
{
    public sealed class NetworkTransform : NetworkBehaviour
    {
        [Header("Sync")]
        [SerializeField] private float sendRate    = 20f; // Hz
        [SerializeField] private bool  syncPosition = true;
        [SerializeField] private bool  syncRotation = true;
        [SerializeField] private bool  syncScale    = false;

        [Header("Interpolation")]
        [SerializeField] private float interpolationSpeed = 15f;

        private Vector3    _targetPosition;
        private Quaternion _targetRotation;
        private float      _sendTimer;

        private bool _isOwner; // true if this client owns this object

        protected override void Start()
        {
            base.Start();
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
        }

        private void Update()
        {
            if (_isOwner)
            {
                _sendTimer += Time.deltaTime;
                if (_sendTimer >= 1f / sendRate)
                {
                    _sendTimer = 0;
                    _ = SendStateAsync();
                }
            }
            else
            {
                // Interpolate towards server position
                transform.position = Vector3.Lerp(
                    transform.position, _targetPosition,
                    Time.deltaTime * interpolationSpeed);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation, _targetRotation,
                    Time.deltaTime * interpolationSpeed);
            }
        }

        private Task SendStateAsync()
        {
            var state = new TransformState
            {
                PosX = transform.position.x,
                PosY = transform.position.y,
                PosZ = transform.position.z,
                RotX = transform.rotation.eulerAngles.x,
                RotY = transform.rotation.eulerAngles.y,
                RotZ = transform.rotation.eulerAngles.z,
            };
            return ServerRpcAsync("SyncTransform", state);
        }

        [ClientRpc]
        private void OnTransformSync(TransformState state)
        {
            if (_isOwner) return;
            if (syncPosition)
                _targetPosition = new Vector3(state.PosX, state.PosY, state.PosZ);
            if (syncRotation)
                _targetRotation = Quaternion.Euler(state.RotX, state.RotY, state.RotZ);
        }
    }

    [Serializable]
    public struct TransformState
    {
        public float PosX, PosY, PosZ;
        public float RotX, RotY, RotZ;
    }
}
