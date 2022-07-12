using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FastTravel
{
    internal class TeleportButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {   
        private TextMeshPro _tmpHighlight;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GameCameras.instance.transform.Find("Teleport Cursor/Text") != null);
            _tmpHighlight = GameCameras.instance.transform.Find("Teleport Cursor/Text").GetComponent<TextMeshPro>();
        }

        private void Update()
        {
            if (InputHandler.Instance.inputActions.menuSubmit.WasPressed && 
                _tmpHighlight.text != "" &&
                USceneManager.GetActiveScene().name != name)
            {
                FastTravel.StartTransition(_tmpHighlight.text, gameObject);
            }
        }

        public void OnPointerClick(PointerEventData _) 
        {
            if (USceneManager.GetActiveScene().name == name) return;

            // This method is in the main class because TeleportButton 
            // becomes inactive once the inventory is closed.
            FastTravel.StartTransition(_tmpHighlight.text, gameObject);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (_tmpHighlight != null) _tmpHighlight.text = name;
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (_tmpHighlight != null) _tmpHighlight.text = "";
        }
    }
}
