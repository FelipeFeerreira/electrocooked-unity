using System.Collections.Generic;
using System.Text;
using ElectroCooked.Circuit;
using ElectroCooked.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.UI
{
    /// <summary>
    /// Painel grande e fixo na tela com o pedido da bancada mais perto do aluno.
    ///
    /// O ticket flutuante em cima da bancada é bonito, mas fica longe da câmera e
    /// pequeno — o aluno acaba tendo que decorar o pedido ou chegar coladinho.
    /// Este painel resolve isso: enquanto você corre atrás das peças, o pedido da
    /// bancada em que você está trabalhando está sempre legível num canto.
    /// </summary>
    public class NearbyStationUI : MonoBehaviour
    {
        [Header("Cena")]
        public Transform player;
        public List<CircuitStation> stations = new List<CircuitStation>();

        [Header("Painel")]
        public GameObject rootPanel;
        public Text headerText;
        public Text bodyText;

        [Header("Alcance")]
        [Tooltip("Distância máxima até a bancada para o painel aparecer.")]
        public float showRange = 6f;

        const string AskColor = "#FFD24A";
        const string MissingColor = "#8FD8FF";
        const string ReadyColor = "#7CFF9E";
        const string FireColor = "#FF7A5C";

        readonly StringBuilder _sb = new StringBuilder();
        CircuitStation _current;
        string _lastRendered;

        void Update()
        {
            if (player == null || rootPanel == null) return;

            CircuitStation best = null;
            float bestDist = showRange;

            for (int i = 0; i < stations.Count; i++)
            {
                CircuitStation s = stations[i];
                if (s == null) continue;
                float d = Vector3.Distance(player.position, s.transform.position);
                if (d >= bestDist) continue;
                bestDist = d;
                best = s;
            }

            _current = best;

            if (_current == null)
            {
                if (rootPanel.activeSelf) rootPanel.SetActive(false);
                _lastRendered = null;
                return;
            }

            if (!rootPanel.activeSelf) rootPanel.SetActive(true);
            Render(_current);
        }

        void Render(CircuitStation station)
        {
            string body = BuildBody(station);
            if (body == _lastRendered) return; // evita remontar a malha de texto todo frame
            _lastRendered = body;

            if (headerText != null) headerText.text = station.name.Replace('_', ' ').ToUpperInvariant();
            if (bodyText != null) bodyText.text = body;
        }

        string BuildBody(CircuitStation station)
        {
            if (station.CurrentState == CircuitStation.State.Broken)
                return $"<b><color={FireColor}>EXPLODIU!</color></b>\nPegue um extintor e apague.";

            if (station.CurrentOrder == null)
                return "Bancada livre.\nAguardando pedido...";

            List<string> lines = station.CurrentOrder.BuildTicketLines();

            _sb.Clear();
            for (int i = 0; i < lines.Count - 1; i++)
                _sb.Append(lines[i]).Append('\n');

            _sb.Append("<b><color=").Append(AskColor).Append('>')
               .Append(lines[lines.Count - 1])
               .Append("</color></b>\n");

            string missing = station.GetMissingPartsDescription();
            bool ready = missing.StartsWith("Montado");
            _sb.Append("<color=").Append(ready ? ReadyColor : MissingColor).Append('>')
               .Append(missing)
               .Append("</color>");

            return _sb.ToString();
        }
    }
}
