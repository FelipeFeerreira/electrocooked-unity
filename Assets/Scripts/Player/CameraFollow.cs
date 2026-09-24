using UnityEngine;

namespace ElectroCooked.Player
{
    /// <summary>
    /// Câmera que segue o aluno de cima, com suavização. Usada no corredor da
    /// escola, que é comprido demais para uma câmera parada.
    ///
    /// Trava opcional por eixo: no corredor a gente deixa a câmera andar só no X,
    /// mantendo o enquadramento estável como num jogo 2.5D.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Alvo")]
        public Transform target;

        [Header("Enquadramento")]
        public Vector3 offset = new Vector3(0f, 11f, -8.5f);
        public float smoothTime = 0.18f;

        [Header("Travas")]
        public bool followX = true;
        public bool followZ = true;

        Vector3 _velocity;

        void Start()
        {
            if (target != null) transform.position = Desired();
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref _velocity, smoothTime);
        }

        Vector3 Desired()
        {
            Vector3 p = target.position + offset;
            if (!followX) p.x = offset.x;
            if (!followZ) p.z = offset.z;
            return p;
        }
    }
}
