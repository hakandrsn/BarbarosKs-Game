using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BarbarosKs.UI
{
    /// <summary>
    /// Loading ekranı UI sistemini yöneten script
    /// SceneController ile entegre çalışır
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text loadingText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text sceneNameText;

        [Header("Loading Animation")]
        [SerializeField] private GameObject loadingSpinner;
        [SerializeField] private float spinSpeed = 180f;

        [Header("Configuration")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        // State
        private bool isLoading = false;
        private string currentTargetScene = "";
        private string currentDescription = "";
        private float currentProgress = 0f;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ [LOADING SCREEN] Initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // UI setup
            SetupLoadingUI();
            
            // Başlangıçta gizle
            HideLoadingScreen();
        }

        private void Update()
        {
            // Loading spinner animation
            if (isLoading && loadingSpinner != null)
            {
                loadingSpinner.transform.Rotate(0, 0, -spinSpeed * Time.deltaTime);
            }
        }

        #region Public Interface

        /// <summary>
        /// Loading işlemini başlatır
        /// </summary>
        public void StartLoading(string targetScene, string description = "")
        {
            currentTargetScene = targetScene;
            currentDescription = description;
            isLoading = true;

            Debug.Log($"🔄 [LOADING SCREEN] Starting load: {targetScene} - {description}");

            ShowLoadingScreen();
            UpdateLoadingText();
        }

        /// <summary>
        /// Loading progress'ini günceller
        /// </summary>
        public void UpdateProgress(float progress, bool canFinish = true)
        {
            currentProgress = Mathf.Clamp01(progress);
            
            if (progressBar != null)
            {
                progressBar.value = currentProgress;
            }

            if (progressText != null)
            {
                progressText.text = $"{(currentProgress * 100):F0}%";
            }

            // Debug log
            Debug.Log($"📊 [LOADING SCREEN] Progress: {currentProgress:F2} - Can finish: {canFinish}");
        }

        /// <summary>
        /// Loading'i tamamlar ve ekranı gizler
        /// </summary>
        public void CompleteLoading()
        {
            Debug.Log("✅ [LOADING SCREEN] Loading completed");
            
            isLoading = false;
            StartCoroutine(FadeOutAndHide());
        }

        /// <summary>
        /// Loading'i anında durdurur
        /// </summary>
        public void StopLoading()
        {
            isLoading = false;
            HideLoadingScreen();
        }

        #endregion

        #region UI Management

        private void SetupLoadingUI()
        {
            if (autoCreateUI && loadingPanel == null)
            {
                CreateBasicLoadingUI();
            }

            // UI validasyonu
            if (loadingPanel == null)
            {
                Debug.LogWarning("⚠️ [LOADING SCREEN] Loading panel not assigned!");
            }
        }

        private void CreateBasicLoadingUI()
        {
            Debug.Log("🏗️ [LOADING SCREEN] Creating basic loading UI");

            // Canvas oluştur
            var canvasGO = new GameObject("LoadingCanvas");
            canvasGO.transform.SetParent(transform);
            
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // En üstte görünsün

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Loading panel
            var panelGO = new GameObject("LoadingPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var image = panelGO.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);

            loadingPanel = panelGO;

            // Loading text
            CreateLoadingText(panelGO.transform);
            
            // Progress bar
            CreateProgressBar(panelGO.transform);

            Debug.Log("✅ [LOADING SCREEN] Basic UI created");
        }

        private void CreateLoadingText(Transform parent)
        {
            var textGO = new GameObject("LoadingText");
            textGO.transform.SetParent(parent, false);

            var rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, 50);
            rectTransform.sizeDelta = new Vector2(400, 50);

            var text = textGO.AddComponent<Text>();
            text.text = "Loading...";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // Default font (Arial)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            loadingText = text;
        }

        private void CreateProgressBar(Transform parent)
        {
            var sliderGO = new GameObject("ProgressBar");
            sliderGO.transform.SetParent(parent, false);

            var rectTransform = sliderGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -50);
            rectTransform.sizeDelta = new Vector2(400, 20);

            var slider = sliderGO.AddComponent<Slider>();
            slider.value = 0f;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f);

            slider.fillRect = fillRect;
            progressBar = slider;

            // Progress text
            var progressTextGO = new GameObject("ProgressText");
            progressTextGO.transform.SetParent(parent, false);
            var progressTextRect = progressTextGO.AddComponent<RectTransform>();
            progressTextRect.anchoredPosition = new Vector2(0, -80);
            progressTextRect.sizeDelta = new Vector2(200, 30);
            var progressTextComp = progressTextGO.AddComponent<Text>();
            progressTextComp.text = "0%";
            progressTextComp.fontSize = 18;
            progressTextComp.color = Color.white;
            progressTextComp.alignment = TextAnchor.MiddleCenter;
            progressTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            progressText = progressTextComp;
        }

        private void ShowLoadingScreen()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                StartCoroutine(FadeIn());
            }
        }

        private void HideLoadingScreen()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        private void UpdateLoadingText()
        {
            if (loadingText != null)
            {
                string text = "Loading...";
                if (!string.IsNullOrEmpty(currentDescription))
                {
                    text = currentDescription;
                }
                loadingText.text = text;
            }

            if (sceneNameText != null && !string.IsNullOrEmpty(currentTargetScene))
            {
                sceneNameText.text = $"Loading: {currentTargetScene}";
            }
        }

        #endregion

        #region Animation

        private IEnumerator FadeIn()
        {
            if (loadingPanel == null) yield break;

            var canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndHide()
        {
            if (loadingPanel == null) yield break;

            var canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            HideLoadingScreen();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug: Test Loading Screen")]
        private void DebugTestLoadingScreen()
        {
            StartCoroutine(TestLoadingSequence());
        }

        private IEnumerator TestLoadingSequence()
        {
            Debug.Log("🧪 Testing loading screen...");
            
            StartLoading("TestScene", "Test loading sequence");
            
            for (int i = 0; i <= 100; i += 10)
            {
                UpdateProgress(i / 100f, i >= 100);
                yield return new WaitForSeconds(0.2f);
            }
            
            yield return new WaitForSeconds(1f);
            CompleteLoading();
        }

        [ContextMenu("Debug: Show Loading Screen")]
        private void DebugShowLoadingScreen()
        {
            StartLoading("DebugScene", "Debug test");
        }

        [ContextMenu("Debug: Hide Loading Screen")]
        private void DebugHideLoadingScreen()
        {
            StopLoading();
        }

        #endregion
    }
} 