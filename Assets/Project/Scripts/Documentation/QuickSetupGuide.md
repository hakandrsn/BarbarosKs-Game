# 🚀 Yeni Architecture - Hızlı Kurulum Rehberi

Bu rehber yeni oluşturulan temiz architecture'u nasıl kuracağınızı adım adım açıklar.

## 📋 Kurulum Önce Gereksinimleri

### **1. Eski Sistemleri Temizleme**
```
⚠️ DİKKAT: Bu adımları yapmadan önce backup alın!

1. GameManager kullanımlarını bulmak için:
   - Edit → Find References in Project → GameManager
   - Tüm kullanımları yeni sistemlere yönlendirin

2. PlayerDataManager → PlayerManager migration
3. GameSystemInitializer → SystemCoordinator migration
```

## 🏗️ 1. Core Asset'leri Oluşturma

### **A) GameSettings Asset**
```
1. Assets → Resources klasörü oluşturun (yoksa)
2. Resources'ta Right Click → Create → BarbarosKs → Game Settings
3. Dosya adı: "GameSettings"
4. Inspector'da configure edin:
   - Projectile Speed: 30
   - Arc Height: 2
   - Max Range: 10
   - Hit Effect Duration: 2
   - Combat Sound Volume: 1
```

### **B) PrefabManager Asset**
```
1. Resources'ta Right Click → Create → BarbarosKs → Prefab Manager
2. Dosya adı: "PrefabManager"
3. Inspector'da prefab'ları atayın:

Cannonball Prefabs:
├── Element 0: CB1
│   ├── ID: 1
│   ├── Type Code: "CB1"
│   ├── Display Name: "Standard Cannonball"
│   ├── Prefab: CB1.prefab
│   ├── Base Damage: 10
│   └── Base Speed: 30
├── Element 1: CB2  
│   ├── ID: 2
│   ├── Type Code: "CB2"
│   ├── Prefab: CB2.prefab
│   └── Base Damage: 15
└── Element 2: Shrapnel
    ├── ID: 3
    ├── Type Code: "SHRAPNEL"
    ├── Prefab: Shrapnel.prefab
    └── Base Damage: 25

Effect Prefabs:
├── Hit Effect: HitEffect.prefab (varsa)
├── Explosion Effect: ExplosionEffect.prefab
└── Lightning Effect: Lightning.prefab

Network Prefabs:
├── Player Prefab: PlayerPrefab (varsa)
└── Network Ship Prefab: NetworkShipPrefab
```

## 🎬 2. Sahne Kurulumları

### **A) Bootstrap Sahnesinde**
```
1. Hierarchy'de Create Empty → "SystemCoordinator"
2. Add Component → SystemCoordinator
3. Inspector Settings:
   ✅ Auto Initialize On Awake
   ✅ Create Missing Systems From Code
   ✅ Enable Bootstrap Systems
   ✅ Enable Gameplay Systems
   ✅ Enable UI Systems
   ✅ Verbose Logging

4. Play → Console'dan sistem başlatma durumunu kontrol edin
```

### **B) Login Sahnesinde**
```
1. SystemCoordinator prefab'ını sahneye ekleyin (yoksa)
2. LoginUI'ı yeni PlayerManager ile entegre edin:

Login başarılı olduğunda:
```csharp
// Eski kod:
GameManager.Instance.OnCharacterDataReceived(characterData);

// Yeni kod:
PlayerManager.Instance.HandleLoginSuccess(characterData);
```

### **C) Ship Selection Sahnesinde**
```
ShipSelectionUI güncellemesi:
```csharp
// Eski kod:
GameManager.Instance.SetActiveShipAndEnterGame(selectedShip);

// Yeni kod:
PlayerManager.Instance.HandleShipSelection(selectedShip);
```

### **D) FisherSea (Game) Sahnesinde**
```
1. SystemCoordinator prefab'ını ekleyin
2. NetworkManager'ı sahneye ekleyin (varsa)
3. Mevcut ProjectileManager'ı update edin:
   - Inspector'da Use Prefab Manager: ✅
   - Fallback prefab'ları atayın
4. PlayerController'ı CombatManager ile entegre edin
```

## 🔧 3. Sistem Entegrasyonları

### **A) ProjectileManager Güncellemesi**
```
Mevcut ProjectileManager'ınızda:
1. Inspector → Use Prefab Manager: ✅
2. Fallback Prefabs atayın:
   - Fallback Cannonball Prefab: CB1.prefab
   - Fallback Shrapnel Prefab: Shrapnel.prefab
3. Test: Context Menu → "Validate PrefabManager Integration"
```

### **B) Combat Sistemleri**
```
1. Hierarchy'de CombatManager otomatik oluşturulacak
2. Inspector'da ayarları kontrol edin:
   - Combat Range: 15
   - Auto Target Range: 10
   - Allow Friendly Fire: ❌
   - Auto Targeting: ✅

3. PlayerController'da combat method'larını güncelleyin:
```csharp
// Eski doğrudan ProjectileManager kullanımı:
ProjectileManager.Instance.SpawnProjectile(...);

// Yeni CombatManager kullanımı:
CombatManager.Instance.FireActiveCannonball();
```

### **C) UI Sistemleri Güncellemesi**
```
AttackButtonController güncellemesi:
```csharp
// Attack button'a basıldığında:
private void OnAttackButtonPressed()
{
    if (CombatManager.Instance != null)
    {
        bool success = CombatManager.Instance.FireActiveCannonball();
        if (success)
        {
            // Attack feedback
        }
    }
}
```

## 🧪 4. Test ve Doğrulama

### **A) Sistem Durumu Testi**
```
1. Play mode'a geçin
2. F1 tuşuna basın → Console'da sistem durumları
3. SystemCoordinator → Inspector → Context Menu → "Validate All Systems"
4. Tüm sistemler ✅ olmalı
```

### **B) Combat Sistemi Testi**
```
1. FisherSea sahnesinde Play
2. CombatManager → Inspector → Context Menu → "Test Cannonball Spawn"
3. Target seçin ve gülle fırlatma test edin
4. SPACE tuşu ile ShrapnelTester test edin
```

### **C) Sahne Geçişi Testi**
```
1. Bootstrap → Login → Ship Selection → Game
2. Her sahne geçişinde Console'dan hata kontrol edin
3. PlayerManager'da data persistency kontrol edin
```

## 📊 5. Performance Monitoring

### **A) Debug Panel'ler**
```
Game View'da sol üstte sistem durumu gösterilmeli:
🎮 GAME SYSTEMS STATUS
✅ GameDataManager
✅ CannonballService  
✅ MarketManager
✅ DataInitializer
✅ ProjectileManager
Data Loaded: True
F1: Check Systems | F2: Load Data
```

### **B) Memory ve Performance**
```
1. Window → Analysis → Profiler
2. SystemCoordinator başlatma sürelerini kontrol edin
3. Memory leaks kontrol edin
4. FPS stability kontrol edin
```

## 🚨 6. Yaygın Sorunlar ve Çözümler

### **Problem: "GameManager bulunamadı" Hatası**
```
Çözüm:
1. Eski GameManager referanslarını bulun
2. PlayerManager.Instance ile değiştirin
3. SceneController.Instance ile sahne yönetimini değiştirin
```

### **Problem: "PrefabManager asset bulunamadı"**
```
Çözüm:
1. Resources/PrefabManager.asset oluşturduğunuzdan emin olun
2. PrefabManager → Context Menu → "Validate All Prefabs"
3. Eksik prefab'ları atayın
```

### **Problem: "SystemCoordinator başlatma hatası"**
```
Debug:
1. SystemCoordinator → Inspector → "Debug: Validate All Systems"
2. Console'da hangi sistem eksik kontrol edin
3. Create Missing Systems From Code: ✅ olduğundan emin olun
```

### **Problem: "Network sistemleri çalışmıyor"**
```
Kontrol:
1. NetworkManager sahne-specific olarak eklenmiş mi?
2. ApiManager başlatılmış mı?
3. Network prefab'ları PrefabManager'da atanmış mı?
```

## ✅ 7. Kurulum Tamamlandı Checklist

- [ ] GameSettings.asset oluşturuldu ve configure edildi
- [ ] PrefabManager.asset oluşturuldu ve tüm prefab'lar atandı
- [ ] Bootstrap sahnesinde SystemCoordinator eklendi
- [ ] Tüm sahnelerde sistem geçişleri test edildi
- [ ] Eski GameManager kullanımları temizlendi
- [ ] ProjectileManager PrefabManager entegrasyonu yapıldı
- [ ] Combat sistemleri test edildi
- [ ] F1/F2 debug tuşları çalışıyor
- [ ] Console'da hata yok
- [ ] Performance monitoring aktif

## 🎯 Son Kontrol

```
Play Mode'da şu komutları test edin:
- F1: Sistem durumu ✅
- F2: Data loading ✅
- SPACE: Shrapnel test ✅
- Combat Manager: Attack test ✅
- Scene transitions: Sorunsuz ✅
```

**Kurulum tamamlandı! 🚀**

Artık temiz, merkezi ve maintainable bir architecture'a sahipsiniz. Yeni özellikler eklerken bu sistemleri kullanarak tutarlı bir codebase sürdürebilirsiniz. 