using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace TestVR
{
    public class BulletSpawner : MonoBehaviour
    {
        private List<Rigidbody> freeBullets = new List<Rigidbody>(5);
        [SerializeField] private Rigidbody _bullet;
        [SerializeField] private Transform _bulletStartPoint;
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private float maxForce;

        private void Start()
        {
            for (int i = 0; i < 5; i++)
            {
                freeBullets.Add(Instantiate(_bullet));
                freeBullets[i].gameObject.SetActive(false);
            }
            grabInteractable.activated.AddListener(Shot);
        }

        private void Shot(ActivateEventArgs arg0)
        {
            var bullet = GetFreeBullet();
            bullet.gameObject.SetActive(true);
            bullet.transform.position = _bulletStartPoint.position;
            var rotation = Quaternion.RotateTowards(_bulletStartPoint.rotation, Random.rotation, 30f);
            bullet.AddForce(rotation * transform.forward * maxForce, ForceMode.Impulse);
            ReturnBulletAsync(bullet, destroyCancellationToken).Forget();
        }

        private Rigidbody GetFreeBullet()
        {
            if (freeBullets.Count > 0)
            {
                var free = freeBullets[^1];
                freeBullets.RemoveAt(freeBullets.Count - 1);
                return free;
            }
            else
            {
                return Instantiate(_bullet);
            }
        }

        private async UniTask ReturnBulletAsync(Rigidbody bullet, CancellationToken token)
        {
            await UniTask.WaitForSeconds(0.5f, cancellationToken: token).SuppressCancellationThrow();
            while (token.IsCancellationRequested == false && (transform.position - _bullet.position).sqrMagnitude > 25)
            {
                await UniTask.WaitForSeconds(0.5f, cancellationToken: token).SuppressCancellationThrow();
            }
            if (token.IsCancellationRequested) return;
            bullet.gameObject.SetActive(false);
            freeBullets.Add(bullet);
        }
    }
}