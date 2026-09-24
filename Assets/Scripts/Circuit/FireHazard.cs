using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElectroCooked.Circuit
{
    /// <summary>
    /// Representa o "circuito pegando fogo" depois de uma conta errada.
    /// Liga partículas/luz de fogo e escuta o extintor até a chama zerar.
    /// </summary>
    public class FireHazard : MonoBehaviour, IExtinguishable
    {
        [Header("Configuração")]
        public float maxFireAmount = 100f;

        [Header("Visual (opcional)")]
        public ParticleSystem fireParticles;
        public Light fireLight;
        public GameObject scorchMarkVisual;

        public float CurrentFireAmount { get; private set; }
        public bool IsOnFire { get; private set; }

        public event Action OnFireStarted;
        public event Action OnFireExtinguished;

        public static readonly List<FireHazard> ActiveHazards = new List<FireHazard>();

        void OnEnable() => ActiveHazards.Add(this);
        void OnDisable() => ActiveHazards.Remove(this);

        public void StartFire(float amount = -1f)
        {
            CurrentFireAmount = amount > 0f ? amount : maxFireAmount;
            IsOnFire = true;
            SetVisual(true);
            OnFireStarted?.Invoke();
        }

        public void ReduceFire(float amount)
        {
            if (!IsOnFire) return;

            CurrentFireAmount -= amount;
            if (CurrentFireAmount <= 0f)
            {
                CurrentFireAmount = 0f;
                IsOnFire = false;
                SetVisual(false);
                OnFireExtinguished?.Invoke();
            }
        }

        void SetVisual(bool onFire)
        {
            if (fireParticles != null)
            {
                if (onFire) fireParticles.Play();
                else fireParticles.Stop();
            }
            if (fireLight != null) fireLight.enabled = onFire;
            if (scorchMarkVisual != null) scorchMarkVisual.SetActive(onFire);
        }
    }
}
