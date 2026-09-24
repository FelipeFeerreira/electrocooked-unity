using ElectroCooked.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.UI
{
    /// <summary>
    /// O "colinha" de fórmulas preso na lateral da tela, como um cartão de
    /// formulário em cima da bancada. Fica visível o tempo todo por padrão
    /// (é material de consulta, não é o desafio) e some/volta com a tecla F.
    ///
    /// O texto é montado aqui no código para ficar num lugar só — mexer no
    /// conteúdo é mexer nesta string, sem precisar editar a cena.
    /// </summary>
    public class FormulaSheetUI : MonoBehaviour
    {
        [Header("Painel")]
        public GameObject rootPanel;
        public Text titleText;
        public Text bodyText;

        [Header("Comportamento")]
        public bool startVisible = true;

        public const string Title = "FORMULARIO  [F]";

        public const string Body =
            "<b>LEI DE OHM</b>\n" +
            "   V = R x I\n" +
            "   I = V / R\n" +
            "   R = V / I\n" +
            "\n" +
            "<b>POTENCIA</b>\n" +
            "   P = V x I\n" +
            "   P = R x I x I\n" +
            "   P = (V x V) / R\n" +
            "\n" +
            "<b>EM SERIE</b>\n" +
            "   Rtotal = R1 + R2 + R3\n" +
            "   I e igual em todos\n" +
            "\n" +
            "<b>UNIDADES</b>\n" +
            "   V = volt        I = ampere\n" +
            "   R = ohm        P = watt\n" +
            "\n" +
            "<b>DICA</b>\n" +
            "   Some os R primeiro,\n" +
            "   depois use a lei de Ohm.";

        void Start()
        {
            if (titleText != null) titleText.text = Title;
            if (bodyText != null) bodyText.text = Body;
            if (rootPanel != null) rootPanel.SetActive(startVisible);
        }

        void Update()
        {
            if (rootPanel == null) return;
            if (InputReader.FormulasPressed)
                rootPanel.SetActive(!rootPanel.activeSelf);
        }
    }
}
