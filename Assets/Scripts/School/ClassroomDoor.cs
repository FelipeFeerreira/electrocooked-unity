using ElectroCooked.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElectroCooked.School
{
    /// <summary>
    /// Uma porta de sala no corredor da escola. Estilo Bully: você anda pelo
    /// corredor, chega perto da porta da matéria e entra na aula.
    ///
    /// A detecção de proximidade é feita pelo SchoolHUD (que procura a porta
    /// mais perto do aluno), não por trigger de física. É mais previsível:
    /// nunca "não entra" porque o collider estava do tamanho errado.
    /// </summary>
    public class ClassroomDoor : MonoBehaviour
    {
        [Header("Identidade da matéria")]
        public string subjectId = SubjectIds.Circuits;
        public string displayName = "CIRCUITOS I";

        [Header("Para onde leva")]
        public string sceneToLoad = SceneNames.CircuitClass;
        public bool isOpen = true;
        public string closedMessage = "Essa matéria ainda não abriu neste semestre.";

        [Header("Alcance")]
        public float interactRange = 3.2f;

        [Header("Visual")]
        public Renderer doorRenderer;
        public Color openColor = new Color(0.20f, 0.75f, 0.45f);
        public Color closedColor = new Color(0.30f, 0.31f, 0.36f);
        public Light doorLight;

        MaterialPropertyBlock _block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        void Start()
        {
            _block = new MaterialPropertyBlock();
            ApplyVisual(false);
        }

        public float DistanceTo(Vector3 position)
            => Vector3.Distance(new Vector3(position.x, 0f, position.z),
                                new Vector3(transform.position.x, 0f, transform.position.z));

        public bool IsInRange(Vector3 position) => DistanceTo(position) <= interactRange;

        /// <summary>Texto grande que aparece na tela quando o aluno chega perto.</summary>
        public string PromptText()
            => isOpen
                ? $"[ E ]   ENTRAR EM {displayName}"
                : $"{displayName} — {closedMessage}";

        /// <summary>Destaca a porta quando o aluno está perto dela.</summary>
        public void SetHighlighted(bool on) => ApplyVisual(on);

        public void Enter()
        {
            if (!isOpen) return;
            if (string.IsNullOrEmpty(sceneToLoad)) return;
            SceneManager.LoadScene(sceneToLoad);
        }

        void ApplyVisual(bool highlighted)
        {
            if (doorLight != null) doorLight.enabled = highlighted && isOpen;
            if (doorRenderer == null) return;

            if (_block == null) _block = new MaterialPropertyBlock();
            Color baseColor = isOpen ? openColor : closedColor;
            Color final = highlighted ? baseColor * 1.6f : baseColor;
            final.a = 1f;

            doorRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, final);
            _block.SetColor(ColorId, final);
            doorRenderer.SetPropertyBlock(_block);
        }
    }
}
