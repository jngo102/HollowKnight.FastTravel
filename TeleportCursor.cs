using GlobalEnums;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FastTravel
{
    internal class TeleportCursor : MonoBehaviour
    {
        private Camera _cam;
        private CircleCollider2D _detectCol;
        private EventSystem _eventSys;

        private void Awake()
        {
            gameObject.name = "Teleport Cursor";
            transform.localScale *= 0.5f;

            _eventSys = gameObject.AddComponent<EventSystem>();
            gameObject.AddComponent<StandaloneInputModule>();

            var highlightedScene = Instantiate(GameManager.instance.inventoryFSM.transform.Find("Map Key/Action/Text").gameObject, transform);
            highlightedScene.name = "Text";
            highlightedScene.transform.SetPosition2D(transform.position + Vector3.up);
            highlightedScene.transform.localScale *= 2;

            var tmp = highlightedScene.GetComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "";
            highlightedScene.GetComponent<MeshRenderer>().material.renderQueue =
                GetComponent<SpriteRenderer>().material.renderQueue = 4001;

            var sceneDetector = new GameObject("Scene Detector");
            sceneDetector.transform.SetParent(transform);
            sceneDetector.layer = (int)PhysLayers.UGUI;
            _detectCol = sceneDetector.AddComponent<CircleCollider2D>();
            _detectCol.isTrigger = true;
            _detectCol.radius = 0.5f;

            _cam = GameCameras.instance.hudCamera;
            _cam.gameObject.AddComponent<Physics2DRaycaster>();
        }

        private void Update()
        {
            if (GameManager.instance.inventoryFSM.transform.Find("Map").gameObject.activeSelf)
            {
                _detectCol.enabled = true;
                EventSystem.current = _eventSys;
                foreach (var rend in GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = true;
                }
                transform.SetPosition2D(_cam.ScreenToWorldPoint(Input.mousePosition));
            }
            else
            {
                _detectCol.enabled = false;
                EventSystem.current = null;
                foreach (var rend in GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = false;
                }
            }
            
        }
    }
}