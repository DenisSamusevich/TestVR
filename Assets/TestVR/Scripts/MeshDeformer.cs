using System;
using System.Collections.Generic;
using UnityEngine;

namespace TestVR
{
    public class MeshDeformer : MonoBehaviour
    {
        private class VerticeCellUpdater
        {
            internal Vector3Int _oldCellPosition;
            internal Vector3Int _newCellPosition;
            internal int _indexVertic;

            internal VerticeCellUpdater(Vector3Int oldCellPosition, Vector3Int newCellPosition, int indexVertic)
            {
                _oldCellPosition = oldCellPosition;
                _newCellPosition = newCellPosition;
                _indexVertic = indexVertic;
            }

            internal void Move(Dictionary<Vector3Int, HashSet<int>> indexVertices)
            {
                indexVertices[_oldCellPosition].Remove(_indexVertic);
                if (indexVertices[_oldCellPosition].Count == 0)
                {
                    indexVertices.Remove(_oldCellPosition);
                }
                if (indexVertices.ContainsKey(_newCellPosition) == false)
                {
                    indexVertices[_newCellPosition] = new HashSet<int>();
                }
                indexVertices[_newCellPosition].Add(_indexVertic);
            }
        }

        public event Action<Collision> OnCollisionByEvent;
        public event Action<ContactPoint> OnCollisionByContactPointEvent;

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshCollider _meshCollider;
        [SerializeField] private BoxCollider _tableCollider;
        [SerializeField] private float sqrImpulseMax;
        private Mesh _originalMesh;
        private Mesh _deformedMesh;
        private Vector3[] _originalVertices;
        private Vector3[] _deformedVertices;
        private Dictionary<Vector3Int, HashSet<int>> _indexVertices = new Dictionary<Vector3Int, HashSet<int>>();
        private int _gridSize = 20;
        const float _radiusForce = 0.1f;
        private List<Vector3Int> _nereastGrid = new List<Vector3Int>();

        private void Start()
        {
            Physics.IgnoreCollision(_tableCollider, _meshCollider);
            _originalMesh = _meshFilter.mesh;
            _deformedMesh = Instantiate(_originalMesh);
            _meshFilter.mesh = _deformedMesh;
            _originalVertices = _originalMesh.vertices;
            _deformedVertices = new Vector3[_originalVertices.Length];
            _meshCollider.sharedMesh = _deformedMesh;
            for (int i = 0; i < _deformedVertices.Length; i++)
            {
                _deformedVertices[i] = _originalVertices[i];
                var vector3Int = GetCoordinate(_deformedVertices[i]);
                if (_indexVertices.ContainsKey(vector3Int) == false)
                {
                    _indexVertices[vector3Int] = new HashSet<int>();
                }
                _indexVertices[vector3Int].Add(i);
            }
            var maxForceCoordinate = GetCoordinate(new Vector3(_radiusForce, _radiusForce, _radiusForce));
            for (int x = -maxForceCoordinate.x; x <= maxForceCoordinate.x; x++)
            {
                for (int y = -maxForceCoordinate.y; y <= maxForceCoordinate.y; y++)
                {
                    for (int z = -maxForceCoordinate.z; z <= maxForceCoordinate.z; z++)
                    {
                        var vectorInt = new Vector3Int(x, y, z);
                        if (vectorInt.magnitude / _gridSize < _radiusForce)
                        {
                            _nereastGrid.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.sqrMagnitude > sqrImpulseMax)
            {
                for (int i = 0; i < collision.contacts.Length && i < 5; i++)
                {
                    UpdateVertices(collision.contacts[UnityEngine.Random.Range(0, collision.contacts.Length)]);
                    OnCollisionByContactPointEvent?.Invoke(collision.contacts[i]);
                }
                OnCollisionByEvent?.Invoke(collision);
                _deformedMesh.vertices = _deformedVertices;
                _deformedMesh.RecalculateNormals();
                _meshCollider.sharedMesh = null;
                _meshCollider.sharedMesh = _deformedMesh;
            }
        }

        private void UpdateVertices(ContactPoint contactPoint)
        {
            var impulse = Vector3.ClampMagnitude(contactPoint.impulse, 0.3f).magnitude;
            var verticeCellUpdaters = new List<VerticeCellUpdater>();
            var contPoint = transform.InverseTransformPoint(contactPoint.point);
            var posInt = GetCoordinate(contPoint);
            for (int i = 0; i < _nereastGrid.Count; i++)
            {
                var currentPoint = posInt + _nereastGrid[i];
                if (_indexVertices.ContainsKey(currentPoint))
                {
                    foreach (var index in _indexVertices[currentPoint])
                    {
                        var distance = Vector3.Distance(_deformedVertices[index], contPoint);
                        if (distance < _radiusForce)
                        {
                            var t = Mathf.InverseLerp(_radiusForce, 0, distance);
                            _deformedVertices[index] += Vector3.Lerp(Vector3.zero, contactPoint.normal * impulse, t);
                            var newPosInt = GetCoordinate(_deformedVertices[index]);
                            if (newPosInt != currentPoint)
                            {
                                var moveToNewCell = new VerticeCellUpdater(currentPoint, newPosInt, index);
                                verticeCellUpdaters.Add(moveToNewCell);
                            }
                        }
                    }
                }
            }
            foreach (var updater in verticeCellUpdaters)
            {
                updater.Move(_indexVertices);
            }
        }

        private Vector3Int GetCoordinate(Vector3 vector3)
        {
            return new Vector3Int(Mathf.RoundToInt(vector3.x * _gridSize), Mathf.RoundToInt(vector3.y * _gridSize), Mathf.RoundToInt(vector3.z * _gridSize));
        }
    }
}
