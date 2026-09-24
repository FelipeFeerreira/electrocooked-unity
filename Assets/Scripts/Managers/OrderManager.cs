using System.Collections;
using System.Collections.Generic;
using ElectroCooked.Circuit;
using ElectroCooked.Core;
using UnityEngine;

namespace ElectroCooked.Managers
{
    /// <summary>
    /// Distribui pedidos (CircuitOrder) para as bancadas livres, igual à fila de
    /// tickets do Overcooked. Aumenta a dificuldade aos poucos conforme a aula avança.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [Header("Bancadas desta sala de aula")]
        public List<CircuitStation> stations = new List<CircuitStation>();

        [Header("Ritmo")]
        [Tooltip("Segundos de 'respiro' antes de um novo pedido cair numa bancada livre.")]
        public float delayBeforeNextOrder = 1.5f;

        [Header("Dificuldade (ajustado pelo GameManager)")]
        [Range(1, 3)] public int difficultyLevel = 1;

        System.Random _rng = new System.Random();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            foreach (var station in stations)
                if (station != null && station.CurrentState == CircuitStation.State.Idle)
                    AssignNewOrder(station);
        }

        public void NotifyStationFreed(CircuitStation station)
        {
            StartCoroutine(AssignAfterDelay(station));
        }

        IEnumerator AssignAfterDelay(CircuitStation station)
        {
            yield return new WaitForSeconds(delayBeforeNextOrder);
            if (station != null) AssignNewOrder(station);
        }

        void AssignNewOrder(CircuitStation station)
        {
            CircuitOrder order = CircuitOrderGenerator.Generate(_rng, difficultyLevel);
            station.AssignOrder(order);
        }

        public void SetDifficulty(int level)
        {
            difficultyLevel = Mathf.Clamp(level, 1, 3);
        }
    }
}
