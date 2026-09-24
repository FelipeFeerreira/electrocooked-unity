using ElectroCooked.Core;
using UnityEngine;

namespace ElectroCooked.Items
{
    /// <summary>
    /// Uma "caixa de suprimentos" infinita — igual às caixas de ingredientes do
    /// Overcooked. O jogador interage com mãos vazias e recebe uma cópia do prefab.
    /// Espalhe várias pela sala: uma de baterias, uma de resistores, uma de fios,
    /// uma de extintores.
    /// </summary>
    public class SupplyCrate : MonoBehaviour
    {
        [Header("O que esta caixa fornece")]
        public Carryable.Kind kind = Carryable.Kind.CircuitPart;
        public CircuitComponentType partType;

        [Header("Prefab a instanciar (deve ter um componente Carryable)")]
        public Carryable itemPrefab;

        [Header("Ponto de saída do item")]
        public Transform spawnPoint;

        public Carryable Spawn()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning($"[SupplyCrate] {name} não tem um prefab configurado.");
                return null;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Carryable instance = Instantiate(itemPrefab, pos, Quaternion.identity);
            instance.kind = kind;
            instance.partType = partType;
            return instance;
        }
    }
}
