using System.Collections.Generic;
using System.Text;
using ElectroCooked.Circuit;
using ElectroCooked.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.UI
{
    /// <summary>
    /// "Ticket" flutuante em cima de uma bancada, mostrando o pedido atual e o
    /// que ainda falta montar — igual às nuvens de pedido do Overcooked.
    ///
    /// A hierarquia é: os dados do circuito em branco, a pergunta em amarelo e
    /// negrito (é o que o aluno precisa achar primeiro), e o que falta pegar em
    /// azul claro embaixo. Ler de relance, de longe, é mais importante do que
    /// caber tudo — por isso as linhas são curtas e espaçadas.
    /// </summary>
    public class OrderTicketUI : MonoBehaviour
    {
        [Header("Referências")]
        public CircuitStation station;
        public Text ticketText;

        [Header("Comportamento")]
        [Tooltip("Manter o ticket alinhado com a tela (copia a rotação da câmera).")]
        public bool faceCamera = true;

        // cores em hexadecimal para o rich text do UnityEngine.UI.Text
        const string AskColor = "#FFD24A";      // amarelo: a pergunta
        const string MissingColor = "#8FD8FF";  // azul claro: o que falta pegar
        const string ReadyColor = "#7CFF9E";    // verde: pode fazer a conta
        const string FireColor = "#FF7A5C";     // vermelho: pegou fogo

        Transform _cam;
        readonly StringBuilder _sb = new StringBuilder();

        void Reset()
        {
            station = GetComponentInParent<CircuitStation>();
            ticketText = GetComponentInChildren<Text>();
        }

        void Awake()
        {
            if (station == null) station = GetComponentInParent<CircuitStation>();
            if (ticketText == null) ticketText = GetComponentInChildren<Text>();
        }

        void OnEnable()
        {
            if (station == null) return;
            station.OnOrderAssigned += HandleOrderAssigned;
            station.OnPartPlaced += HandleStationChanged;
            station.OnOrderDelivered += HandleStationChanged;
            station.OnExploded += HandleExploded;
            station.OnFireOut += HandleStationChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (station == null) return;
            station.OnOrderAssigned -= HandleOrderAssigned;
            station.OnPartPlaced -= HandleStationChanged;
            station.OnOrderDelivered -= HandleStationChanged;
            station.OnExploded -= HandleExploded;
            station.OnFireOut -= HandleStationChanged;
        }

        void LateUpdate()
        {
            if (!faceCamera) return;
            if (_cam == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                _cam = main.transform;
            }
            // copia a rotação da câmera: fica reto na tela, não torto
            transform.rotation = _cam.rotation;
        }

        void HandleOrderAssigned(CircuitStation s, CircuitOrder order) => Refresh();
        void HandleStationChanged(CircuitStation s) => Refresh();

        void HandleExploded(CircuitStation s)
        {
            if (ticketText == null) return;
            ticketText.text =
                $"<b><color={FireColor}>EXPLODIU!</color></b>\n\n" +
                $"<color={FireColor}>Pegue um extintor\ne apague o fogo</color>";
        }

        void Refresh()
        {
            if (ticketText == null || station == null) return;

            if (station.CurrentState == CircuitStation.State.Broken)
            {
                HandleExploded(station);
                return;
            }

            if (station.CurrentOrder == null)
            {
                ticketText.text = "<b>BANCADA LIVRE</b>\n\nAguardando o\npróximo pedido...";
                return;
            }

            List<string> lines = station.CurrentOrder.BuildTicketLines();

            _sb.Clear();
            _sb.Append("<b>PEDIDO</b>\n\n");

            // todas as linhas menos a última são os dados do circuito
            for (int i = 0; i < lines.Count - 1; i++)
                _sb.Append(lines[i]).Append('\n');

            // a última linha é a pergunta: destaque máximo
            _sb.Append('\n')
               .Append("<b><color=").Append(AskColor).Append('>')
               .Append(lines[lines.Count - 1])
               .Append("</color></b>\n\n");

            string missing = station.GetMissingPartsDescription();
            bool ready = missing.StartsWith("Montado");
            _sb.Append("<color=").Append(ready ? ReadyColor : MissingColor).Append('>')
               .Append(missing)
               .Append("</color>");

            ticketText.text = _sb.ToString();
        }
    }
}
