using ElectroCooked.Circuit;
using UnityEngine;

namespace ElectroCooked.Items
{
    /// <summary>
    /// Comportamento do extintor de incêndio enquanto está na mão do jogador.
    /// Segure o botão de uso (Espaço / clique / gatilho) apontado para uma estação
    /// em chamas para apagar o fogo, igual ao extintor do Overcooked.
    /// A carga é limitada — quando acaba, pegue outro na caixa vermelha.
    /// </summary>
    public class FireExtinguisherItem : MonoBehaviour
    {
        [Header("Alcance e desempenho")]
        public float range = 3.2f;
        public float extinguishPerSecond = 45f;  // fireAmount reduzido por segundo de uso
        public float maxCharge = 100f;
        public float chargeUsedPerSecond = 14f;
        [Tooltip("Quão de frente o alvo precisa estar. 1 = exatamente à frente, 0 = até 90° de lado.")]
        [Range(0f, 1f)] public float aimTolerance = 0.2f;

        [Header("Visual (opcional)")]
        public ParticleSystem sprayParticles;

        [Header("Estado (runtime)")]
        public float currentCharge;

        bool _spraying;

        public bool HasCharge => currentCharge > 0f;
        public float ChargeNormalized => maxCharge <= 0f ? 0f : Mathf.Clamp01(currentCharge / maxCharge);

        void Awake()
        {
            currentCharge = maxCharge;
        }

        /// <summary>
        /// Chamado a cada frame enquanto o jogador segura o botão de uso.
        /// Procura o alvo apagável mais próximo dentro do alcance.
        /// </summary>
        public bool TryExtinguish(Vector3 fromPosition, Vector3 forward, float deltaTime)
        {
            if (!HasCharge) return false;

            IExtinguishable target = FindClosestTarget(fromPosition, forward);
            if (target == null) return false;

            target.ReduceFire(extinguishPerSecond * deltaTime);

            currentCharge = Mathf.Max(0f, currentCharge - chargeUsedPerSecond * deltaTime);
            return true;
        }

        /// <summary>Liga/desliga o jato de CO2 (partículas), sem custar carga.</summary>
        public void SetSprayVisual(bool on)
        {
            if (_spraying == on) return;
            _spraying = on;
            if (sprayParticles == null) return;

            if (on) sprayParticles.Play();
            else sprayParticles.Stop();
        }

        IExtinguishable FindClosestTarget(Vector3 fromPosition, Vector3 forward)
        {
            IExtinguishable best = null;
            float bestDist = range;
            Vector3 fwd = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;

            var hazards = FireHazard.ActiveHazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                FireHazard hazard = hazards[i];
                if (hazard == null || !hazard.IsOnFire) continue;

                Vector3 delta = hazard.transform.position - fromPosition;
                float dist = delta.magnitude;
                if (dist > range || dist >= bestDist) continue;

                if (Vector3.Dot(fwd, delta.normalized) < aimTolerance) continue; // precisa estar apontando pra lá

                bestDist = dist;
                best = hazard;
            }
            return best;
        }
    }
}
