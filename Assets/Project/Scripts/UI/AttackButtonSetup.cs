using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BarbarosKs.UI
{
    /// <summary>
    /// Unity Editor'da Attack Button UI'sını otomatik olarak oluşturan helper class
    /// </summary>
    public class AttackButtonSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        /// <summary>
        /// Runtime'da otomatik UI setup'ı yapar
        /// </summary>
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupAttackButtonUI();
            }
        }
        
        /// <summary>
        /// Attack Button UI'sını programmatik olarak oluşturur
        /// </summary>
        [ContextMenu("Setup Attack Button UI")]
        public void SetupAttackButtonUI()
        {
            // Mevcut Canvas'ı bul veya oluştur
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.Log("🎨 [UI SETUP] Canvas bulunamadı, yeni Canvas oluşturuluyor...");
                canvas = CreateCanvas();
            }
            
            // Mevcut Attack Button'ı kontrol et
            AttackButtonController existingButton = FindObjectOfType<AttackButtonController>();
            if (existingButton != null)
            {
                Debug.Log("✅ [UI SETUP] Attack Button zaten mevcut!");
                return;
            }
            
            // Attack Button oluştur
            CreateAttackButton(canvas);
            
            Debug.Log("🎯 [UI SETUP] Attack Button UI başarıyla oluşturuldu!");
        }
        
        /// <summary>
        /// Yeni Canvas oluşturur
        /// </summary>
        private Canvas CreateCanvas()
        {
            // Canvas GameObject oluştur
            GameObject canvasObj = new GameObject("Game UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // En üstte görünsün
            
            // CanvasScaler ekle
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // GraphicRaycaster ekle
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("🖼️ [UI SETUP] Yeni Canvas oluşturuldu");
            return canvas;
        }
        
        /// <summary>
        /// Attack Button UI'sını oluşturur
        /// </summary>
        private void CreateAttackButton(Canvas canvas)
        {
            // Ana buton GameObject'i oluştur
            GameObject buttonObj = new GameObject("AttackButton");
            buttonObj.transform.SetParent(canvas.transform, false);
            
            // RectTransform ayarları
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0f); // Sağ alt
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-120f, 120f); // Sağ alttan 120px içeride
            buttonRect.sizeDelta = new Vector2(100f, 100f); // 100x100 boyut
            
            // Image component (buton background)
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.white;
            
            // Button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Text oluştur
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // TextMeshPro kullanmaya çalış, yoksa Text kullan
            TextMeshProUGUI textMesh = textObj.AddComponent<TextMeshProUGUI>();
            if (textMesh != null)
            {
                textMesh.text = "Saldırı\\nPasif";
                textMesh.fontSize = 14;
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.color = Color.black;
            }
            
            // AttackButtonController component'i ekle
            AttackButtonController controller = buttonObj.AddComponent<AttackButtonController>();
            
            // Controller ayarlarını yap
            SetupControllerReferences(controller, button, textMesh, buttonImage);
            
            Debug.Log("🔫 [UI SETUP] Attack Button oluşturuldu!");
        }
        
        /// <summary>
        /// AttackButtonController referanslarını ayarlar
        /// </summary>
        private void SetupControllerReferences(AttackButtonController controller, Button button, TextMeshProUGUI text, Image image)
        {
            // Public field'ları direkt ayarla
            controller.attackButton = button;
            controller.buttonText = text;
            controller.buttonIcon = image;
            
            Debug.Log("🔗 [UI SETUP] Controller referansları ayarlandı");
        }
    }
} 