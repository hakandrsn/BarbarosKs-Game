using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace BarbarosKs.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image loadingIcon;

        [Header("Loading Settings")]
        [SerializeField] private float iconRotationSpeed = 180f;
        [SerializeField] private float textAnimationSpeed = 0.5f;

        private static LoadingScreen instance;
        public static LoadingScreen Instance => instance;

        private Coroutine loadingTextAnimationCoroutine;
        private Coroutine loadingIconAnimationCoroutine;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                HideLoading();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowLoading(string message = "Yükleniyor...")
        {
            Debug.Log($"[LoadingScreen] Loading gösteriliyor: {message}");
            
            loadingPanel.SetActive(true);
            SetLoadingText(message);
            SetProgress(0f);
            
            StartAnimations();
        }

        public void HideLoading()
        {
            Debug.Log("[LoadingScreen] Loading gizleniyor");
            
            loadingPanel.SetActive(false);
            StopAnimations();
        }

        public void SetLoadingText(string text)
        {
            if (loadingText != null)
            {
                loadingText.text = text;
            }
        }

        public void SetProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
            
            if (progressText != null)
            {
                progressText.text = $"%{(progress * 100):F0}";
            }
        }

        public void UpdateLoadingStep(string step, float progress)
        {
            SetLoadingText(step);
            SetProgress(progress);
            Debug.Log($"[LoadingScreen] Loading step: {step} (%{progress * 100:F0})");
        }

        private void StartAnimations()
        {
            StopAnimations();
            
            if (loadingIcon != null)
            {
                loadingIconAnimationCoroutine = StartCoroutine(RotateLoadingIcon());
            }
            
            if (loadingText != null)
            {
                loadingTextAnimationCoroutine = StartCoroutine(AnimateLoadingText());
            }
        }

        private void StopAnimations()
        {
            if (loadingIconAnimationCoroutine != null)
            {
                StopCoroutine(loadingIconAnimationCoroutine);
                loadingIconAnimationCoroutine = null;
            }
            
            if (loadingTextAnimationCoroutine != null)
            {
                StopCoroutine(loadingTextAnimationCoroutine);
                loadingTextAnimationCoroutine = null;
            }
        }

        private IEnumerator RotateLoadingIcon()
        {
            while (true)
            {
                if (loadingIcon != null)
                {
                    loadingIcon.transform.Rotate(0, 0, -iconRotationSpeed * Time.deltaTime);
                }
                yield return null;
            }
        }

        private IEnumerator AnimateLoadingText()
        {
            string baseText = loadingText.text;
            if (baseText.EndsWith("..."))
            {
                baseText = baseText.Substring(0, baseText.Length - 3);
            }

            int dotCount = 0;
            
            while (true)
            {
                string dots = new string('.', dotCount + 1);
                loadingText.text = baseText + dots;
                
                dotCount = (dotCount + 1) % 4;
                yield return new WaitForSeconds(textAnimationSpeed);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
} 