// Enemies klasörü içinde olduğumuz için bu namespace'i ekliyoruz.
// Bu sayede diğer script'ler bu sınıfa Enemies.EnemyAI olarak erişebilir.

using UnityEngine;

namespace Enemies
{
    /// <summary>
    ///     Düşman nesnelerine eklenecek olan yapay zeka ve temel özellikleri
    ///     yönetecek olan ana script.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("Düşman Ayarları")] public float can = 100f;

        public float hareketHizi = 5f;
        public string dusmanTipi = "Piyade";

        // Bu script'in varlığı, bir nesnenin "düşman" olarak tanınması için yeterlidir.
        // Gelecekte düşman yapay zekasını (hareket, saldırı vb.) bu script içine yazacaksınız.

        private void Start()
        {
            // Düşman oluşturulduğunda yapılacak başlangıç ayarları.
            // Örneğin, can barını ayarlama, hedef oyuncuyu bulma vb.
        }

        private void Update()
        {
            // Her frame'de çalışacak olan düşman davranışları.
            // Örneğin, oyuncuya doğru hareket etme, menzile girince saldırma vb.
        }

        public void HasarAl(float miktar)
        {
            can -= miktar;
            if (can <= 0)
                // Düşman yok olduğunda yapılacaklar.
                // Örneğin, patlama efekti, puan ekleme vb.
                Destroy(gameObject);
        }
    }
}