🎮 BarbarosKs: MMO Game Client
BarbarosKs MMO projesinin Unity ile geliştirilen oyun istemcisidir (Client). Bu proje, oyuncuların oyun dünyasına girmesini, etkileşimde bulunmasını ve gerçek zamanlı (Real-Time) deneyimi yaşamasını sağlar.

⚠️ Bağımlılık Uyarısı: Bu proje tek başına çalışmaz. Login olabilmek ve verileri kaydetmek için BarbarosKs Backend API servisinin ayakta olması gerekir.

🕹️ Özellikler (Features)
Oyun Motoru: Unity 2022.3 LTS (veya senin sürümün)

Networking (Gameplay): Netcode for GameObjects (NGO) / Mirror (Kullandığını buraya yaz)

Backend İletişimi: REST API (JSON Web Token Auth)

UI Mimari: MVC / MVVM Pattern (Toolkit veya uGUI)

🏗️ Mimari Yapı
Proje, görsel ve mantıksal katmanları birbirinden ayırmak için modüler bir yapı kullanır:

/Network: Backend API ile konuşan AuthService, InventoryService gibi HTTP istemcileri.

/Systems: Envanter, Yetenek ve Karakter sistemleri (ScriptableObject tabanlı).

/UI: Kullanıcı arayüzü ve ViewModel bağlantıları.

🚀 Kurulum ve Oynama
Backend'i Başlatın: Önce BarbarosKs API projesini çalıştırın (https://localhost:5001).

Unity Projesini Açın:

Unity Hub üzerinden projeyi açın.

Versiyon uyuşmazlığı olursa "Install/Update" seçeneğini kullanın.

API Bağlantısını Ayarlayın:

Assets/_Project/Resources/GameConfig (veya benzeri bir ScriptableObject) dosyasını bulun.

Base URL kısmına kendi API adresinizi girin (Örn: http://localhost:5000).

Oyunu Başlatın:

Scenes/LoginScene sahnesini açın ve Play tuşuna basın.

📦 Kullanılan Varlıklar (Assets & Packages)
DOTween: Animasyonlar için.

UniTask: Asenkron işlemler (async/await) için.

Newtonsoft.Json: JSON serileştirme işlemleri için.

🔗 Backend Reposu
Oyunun sunucu tarafı, veritabanı ve iş mantığı için: 👉 BarbarosKs API

👨‍💻 Geliştirici
Hakan Dursun - Game Developer LinkedIn | GitHub
