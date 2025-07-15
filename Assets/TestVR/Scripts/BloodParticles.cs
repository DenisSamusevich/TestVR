using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace TestVR
{
    public class BloodParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _prefabEffect;
        [SerializeField] private MeshDeformer _meshDeformer;
        private List<ParticleSystem> freeParticleSystems = new List<ParticleSystem>(5);

        private void Start()
        {
            for (int i = 0; i < 5; i++)
            {
                freeParticleSystems.Add(Instantiate(_prefabEffect));
            }
            _meshDeformer.OnCollisionByContactPointEvent += ActivateBlood;
        }

        private void OnDestroy()
        {
            _meshDeformer.OnCollisionByContactPointEvent -= ActivateBlood;
        }

        private void ActivateBlood(ContactPoint contactPoint)
        {
            var system = GetFreeParticle();
            system.transform.position = contactPoint.point;
            system.transform.forward = -contactPoint.normal;
            system.Play();
            ReturnSystemAsync(system, destroyCancellationToken).Forget();
        }

        private ParticleSystem GetFreeParticle()
        {
            if (freeParticleSystems.Count > 0)
            {
                var free = freeParticleSystems[^1];
                freeParticleSystems.RemoveAt(freeParticleSystems.Count - 1);
                return free;
            }
            else
            {
                return Instantiate(_prefabEffect);
            }
        }

        private async UniTask ReturnSystemAsync(ParticleSystem system, CancellationToken token)
        {
            await UniTask.WaitForSeconds(0.5f, cancellationToken: token).SuppressCancellationThrow();
            while (token.IsCancellationRequested == false && system.isPlaying)
            {
                await UniTask.WaitForSeconds(0.5f, cancellationToken: token).SuppressCancellationThrow();
            }
            if (token.IsCancellationRequested) return;
            freeParticleSystems.Add(system);
        }
    }
}