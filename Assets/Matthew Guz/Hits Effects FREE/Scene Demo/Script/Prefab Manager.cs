using UnityEngine;
using UnityEngine.UI;

namespace MatthewAssets
{
    public class PrefabManager : MonoBehaviour
    {
        public GameObject[] prefabs; // List of prefabs assigned from the Inspector
        public Collider floorCollider; // The floor to detect clicks
        public Transform cameraPivot; // Pivot for the camera
        public float cameraRotationSpeed = 10f; // Rotation speed
        public float destroyDelay = 2f; // Time to destroy prefabs
        public Text infoText;
        private int currentIndex; // Index of the current prefab


        private void Start()
        {
            // Guard against missing array
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("⚠️ [MATTHEW PREFAB MANAGER] Prefabs array boş!");
                return;
            }
            UpdateInfoText(); // Update text at start
        }

        private void Update()
        {
            // DISABLED: Input System Package conflict fix
            // These calls cause InvalidOperationException with new Input System
            
            /*
            // DISABLED: Input System conflict fix
            // if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) SelectPreviousPrefab();
            // if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) SelectNextPrefab();

            // DISABLED: Input System conflict fix
            /* if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (floorCollider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000f))
                {
                    var instance = Instantiate(prefabs[currentIndex], hit.point, Quaternion.identity);
                    Destroy(instance, destroyDelay); // Destroy in 2 seconds
                }
            } */

            // Camera rotation still works
            if (cameraPivot != null)
            {
                cameraPivot.Rotate(Vector3.up * (cameraRotationSpeed * Time.deltaTime)); // Rotates on the Y axis
            }
        }


        private void SelectPreviousPrefab() // Previous prefab
        {
            if (prefabs == null || prefabs.Length == 0) return;
            currentIndex--;
            if (currentIndex < 0) currentIndex = prefabs.Length - 1;
            UpdateInfoText();
        }

        private void SelectNextPrefab() // Next prefab
        {
            if (prefabs == null || prefabs.Length == 0) return;
            currentIndex++;
            if (currentIndex >= prefabs.Length) currentIndex = 0;
            UpdateInfoText();
        }

        private void UpdateInfoText() // Name and number of the prefab
        {
            if (prefabs == null || prefabs.Length == 0 || infoText == null) return;
            
            var currentNumber = currentIndex + 1;
            var totalNumber = prefabs.Length;

            infoText.text = $"({currentNumber}/{totalNumber}) \nCurrent effect: {prefabs[currentIndex].name} ";
        }
    }
}