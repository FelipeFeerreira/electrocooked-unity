using System.Collections.Generic;
using System.Text;
using ElectroCooked.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ElectroCooked.School
{
    /// <summary>
    /// A interface do corredor da escola: título, o aviso grande de "aperte E
    /// para entrar" e o mural de notas.
    ///
    /// Também é quem decide de qual porta o aluno está perto — assim existe um
    /// único lugar que resolve a interação, e não uma porta brigando com a outra.
    /// </summary>
    public class SchoolHUD : MonoBehaviour
    {
        [Header("Cena")]
        public Transform player;
        public List<ClassroomDoor> doors = new List<ClassroomDoor>();

        [Header("Textos")]
        public Text titleText;
        public Text promptText;
        public Text reportText;
        public Text hintText;

        [Header("Cores do aviso")]
        public Color openPromptColor = new Color(0.45f, 1f, 0.60f);
        public Color closedPromptColor = new Color(0.95f, 0.65f, 0.35f);

        readonly StringBuilder _sb = new StringBuilder();
        ClassroomDoor _nearest;

        void Start()
        {
            if (titleText != null) titleText.text = "COLEGIO ELECTRO  —  CORREDOR";
            if (hintText != null) hintText.text = "WASD andar   |   E entrar na sala";
            RefreshReport();
        }

        void Update()
        {
            if (player == null) return;

            ClassroomDoor best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < doors.Count; i++)
            {
                ClassroomDoor door = doors[i];
                if (door == null) continue;
                float d = door.DistanceTo(player.position);
                if (d > door.interactRange || d >= bestDist) continue;
                bestDist = d;
                best = door;
            }

            if (best != _nearest)
            {
                if (_nearest != null) _nearest.SetHighlighted(false);
                _nearest = best;
                if (_nearest != null) _nearest.SetHighlighted(true);
            }

            if (promptText != null)
            {
                if (_nearest == null)
                {
                    promptText.text = "";
                }
                else
                {
                    promptText.text = _nearest.PromptText();
                    promptText.color = _nearest.isOpen ? openPromptColor : closedPromptColor;
                }
            }

            if (_nearest != null && _nearest.isOpen && InputReader.InteractPressed)
                _nearest.Enter();
        }

        void RefreshReport()
        {
            if (reportText == null) return;

            _sb.Clear();
            _sb.Append("<b>BOLETIM</b>\n");

            for (int i = 0; i < doors.Count; i++)
            {
                ClassroomDoor door = doors[i];
                if (door == null) continue;

                if (!StudentRecord.HasTaken(door.subjectId))
                {
                    _sb.Append(door.displayName).Append(":  --\n");
                    continue;
                }

                float best = StudentRecord.BestGrade(door.subjectId);
                _sb.Append(door.displayName)
                   .Append(":  ")
                   .Append(best.ToString("0.0"))
                   .Append("  (")
                   .Append(StudentRecord.Concept(best))
                   .Append(")\n");
            }

            reportText.text = _sb.ToString();
        }
    }
}
