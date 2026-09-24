using UnityEngine;

namespace ElectroCooked.UI
{
    /// <summary>
    /// Mantém um objeto de mundo (placa, ticket) alinhado com a tela.
    ///
    /// Importante: copia a ROTAÇÃO da câmera em vez de "olhar para" ela. Olhar
    /// para a câmera faz cada placa girar um pouco diferente conforme a posição
    /// dela na sala — foi isso que deixou as placas tortas e difíceis de ler.
    /// Copiando a rotação, todo texto fica paralelo à tela e perfeitamente reto.
    /// </summary>
    [ExecuteAlways]
    public class Billboard : MonoBehaviour
    {
        Transform _cam;

        void LateUpdate()
        {
            if (_cam == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                _cam = main.transform;
            }
            transform.rotation = _cam.rotation;
        }
    }
}
