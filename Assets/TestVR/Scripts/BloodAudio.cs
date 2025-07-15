using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace TestVR
{
    public class BloodAudio : MonoBehaviour
    {
        [SerializeField] private MeshDeformer _meshDeformer;
        [SerializeField] List<AudioSource> freeAudioSource = new List<AudioSource>();

        private void Start()
        {
            _meshDeformer.OnCollisionByEvent += ActivateBloodAudio;
        }

        private void OnDestroy()
        {
            _meshDeformer.OnCollisionByEvent -= ActivateBloodAudio;
        }

        private void ActivateBloodAudio(Collision collision)
        {
            if (TryGetFreeAudio(out AudioSource audio))
            {
                audio.Play();
                ReturnSystemAsync(audio, destroyCancellationToken).Forget();
            }
        }

        private bool TryGetFreeAudio(out AudioSource audio)
        {
            if (freeAudioSource.Count > 0)
            {
                var index = Random.Range(0, freeAudioSource.Count);
                audio = freeAudioSource[index];
                freeAudioSource.RemoveAt(index);
                return true;
            }
            audio = null;
            return false;
        }

        private async UniTask ReturnSystemAsync(AudioSource audio, CancellationToken token)
        {
            await UniTask.WaitForSeconds(0.3f, cancellationToken: token).SuppressCancellationThrow();
            while (token.IsCancellationRequested == false && audio.isPlaying)
            {
                await UniTask.WaitForSeconds(0.3f, cancellationToken: token).SuppressCancellationThrow();
            }
            if (token.IsCancellationRequested) return;
            freeAudioSource.Add(audio);
        }
    }
}