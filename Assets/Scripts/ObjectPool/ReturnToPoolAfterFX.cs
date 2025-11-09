// Filename: ReturnToPoolAfterFx.cs
using UnityEngine;
using System.Collections;
using EchoMage.Core;
using BillUtils.ObjectPooler;

namespace YourProject.ObjectPooling
{
    [AddComponentMenu("Pooling/Return To Pool After FX")]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ReturnToPoolAfterFx : MonoBehaviour
    {
        private ParticleSystem _particleSystemComponent;

        private void Awake()
        {
            _particleSystemComponent = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            StartCoroutine(CheckIfAlive());
        }

        private IEnumerator CheckIfAlive()
        {
            // Đợi cho đến khi Particle System thực sự dừng hoàn toàn (bao gồm cả các hạt con)
            yield return new WaitUntil(() => !_particleSystemComponent.IsAlive(true));
            ObjectPoolManager.Instance.Despawn(gameObject);
        }
    }
}