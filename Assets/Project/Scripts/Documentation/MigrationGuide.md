# 🔄 Migration Guide: Eski Sistemden Yeni Architecture'a Geçiş

Bu rehber mevcut Unity projesindeki eski sistemleri yeni architecture'a nasıl migrate edeceğinizi açıklar.

## 📊 Migration Önce-Sonra Karşılaştırması

### **ESKİ SİSTEM (Deprecated)**
```
❌ GameManager - Karışık sorumluluklar
❌ PlayerDataManager - Tek amaçlı
❌ GameSystemInitializer - Basit başlatma
❌ ProjectileManager - Manual prefab assignment
❌ Dağınık scene management - 6 farklı yerde
❌ API çağrıları dağınık
❌ Singleton chaos (15+ manager)
❌ No centralized prefab management
❌ No game state coordination
```

### **YENİ SİSTEM (Clean Architecture)**
```
✅ SceneController - Merkezi sahne yönetimi
✅ PlayerManager - Konsolide player/ship yönetimi
✅ SystemCoordinator - Organize sistem başlatma
✅ PrefabManager - ScriptableObject-based prefab management
✅ CombatManager - Merkezi combat coordination
✅ GameStateManager - State machine pattern
✅ Düzenli singleton pattern
✅ Scene-aware initialization
✅ Centralized API management
✅ Clean separation of concerns
```

---

## 🔄 Adım Adım Migration

### **1. PHASE 1: Core System Migration**

#### **A) GameManager → PlayerManager + SceneController**

**Eski kod bulma:**
```bash
# Unity'de Find References kullanın:
Edit → Find References in Project → GameManager
```

**Migration mapping:**
```csharp
// ESKİ - GameManager
GameManager.Instance.OnCharacterDataReceived(data);
GameManager.Instance.SetActiveShipAndEnterGame(ship);
GameManager.Instance.ToScene("FisherSea");
GameManager.Instance.CurrentPlayerProfile;
GameManager.Instance.ActiveShip;

// YENİ - PlayerManager + SceneController
PlayerManager.Instance.HandleLoginSuccess(data);
PlayerManager.Instance.HandleShipSelection(ship);
SceneController.Instance.LoadScene("FisherSea");
PlayerManager.Instance.PlayerProfile;
PlayerManager.Instance.ActiveShip;
```

**Değiştirilmesi gereken dosyalar:**
- `LoginUI.cs`
- `RegisterUI.cs`
- `ShipSelectionUI.cs`
- Network event handlers
- Any custom UI scripts

#### **B) PlayerDataManager → PlayerManager**

**Property mapping:**
```csharp
// ESKİ - PlayerDataManager
PlayerDataManager.Instance.PlayerProfile
PlayerDataManager.Instance.OwnedShips
PlayerDataManager.Instance.ActiveShip
PlayerDataManager.Instance.LoadPlayerData(data)
PlayerDataManager.Instance.SetActiveShip(ship)

// YENİ - PlayerManager
PlayerManager.Instance.PlayerProfile
PlayerManager.Instance.OwnedShips
PlayerManager.Instance.ActiveShip
PlayerManager.Instance.LoadPlayerData(data)
PlayerManager.Instance.SetActiveShip(ship)
```

**Ek özellikler (YENİ):**
```csharp
// YENİ özellikler PlayerManager'da:
PlayerManager.Instance.HasPlayerData
PlayerManager.Instance.HasActiveShip
PlayerManager.Instance.IsInGame
PlayerManager.Instance.LastKnownPosition
PlayerManager.Instance.LastKnownHealth
PlayerManager.Instance.GetPlayerId()
PlayerManager.Instance.EnterGame()
PlayerManager.Instance.ExitGame()
```

#### **C) GameSystemInitializer → SystemCoordinator**

**GameSystemInitializer GameObject'lerini bulun:**
```
Hierarchy'de arama: "GameSystemInitializer"
Tüm sahnelerde kontrol edin
```

**Migration steps:**
1. Eski GameSystemInitializer'ları silin
2. SystemCoordinator prefab'ı oluşturun
3. Tüm sahnelere SystemCoordinator ekleyin

### **2. PHASE 2: Combat System Migration**

#### **A) ProjectileManager Updates**

**Manuel prefab assignment → PrefabManager:**
```csharp
// ESKİ - Manuel atama
[SerializeField] private GameObject cannonballPrefab;
[SerializeField] private GameObject shrapnelPrefab;

// YENİ - PrefabManager integration
[SerializeField] private bool usePrefabManager = true;
[SerializeField] private GameObject fallbackCannonballPrefab; // backup
```

**Method updates:**
```csharp
// ESKİ - Direct projectile spawning
ProjectileManager.Instance.SpawnNetworkProjectile(data, prefab);

// YENİ - Enhanced methods
ProjectileManager.Instance.SpawnProjectile(cannonballData, pos, target);
ProjectileManager.Instance.SpawnProjectile("CB1", pos, target, damage);
```

#### **B) Combat Logic → CombatManager**

**Eski dağınık combat kodu:**
```csharp
// Eski combat kodları (PlayerController, AttackButton, etc.)
if (target != null && inRange)
{
    var projectile = Instantiate(cannonballPrefab);
    projectile.GetComponent<Projectile>().Initialize(damage, target);
}
```

**Yeni merkezi combat:**
```csharp
// YENİ - CombatManager üzerinden
CombatManager.Instance.SetTarget(target);
CombatManager.Instance.FireActiveCannonball();
// veya
CombatManager.Instance.FireProjectile(cannonballData);
```

### **3. PHASE 3: Scene Management Migration**

#### **A) Scene Transition Code Updates**

**SceneManager.LoadScene çağrılarını bulun:**
```bash
# Search in project:
SceneManager.LoadScene
UnityEngine.SceneManagement.SceneManager.LoadScene
```

**Migration examples:**
```csharp
// ESKİ - Direct scene loading
SceneManager.LoadScene("Login");
SceneManager.LoadScene("FisherSea");
SceneManager.LoadSceneAsync("Loading");

// YENİ - SceneController
SceneController.Instance.LoadLogin();
SceneController.Instance.LoadGame();
SceneController.Instance.LoadScene("Loading", "Data yükleniyor");
```

#### **B) Scene-Specific Logic**

**Bootstrap scene:**
```csharp
// ESKİ - Manual sequencing
void Start()
{
    InitializeSystems();
    Invoke("LoadLogin", 2f);
}

// YENİ - SystemCoordinator handles this
// Otomatik sistem başlatma ve login'e geçiş
```

**Game scene:**
```csharp
// ESKİ - Manual game setup
void Start()
{
    SetupPlayer();
    SetupNetwork();
    StartGame();
}

// YENİ - GameStateManager integration
// SystemCoordinator otomatik game setup
// GameStateManager.OnGameStateChanged events
```

### **4. PHASE 4: UI System Migration**

#### **A) Login/Register UI Updates**

**LoginUI.cs example:**
```csharp
// ESKİ
private void OnLoginSuccess(CharacterSelectionDto data)
{
    GameManager.Instance.OnCharacterDataReceived(data);
}

// YENİ
private void OnLoginSuccess(CharacterSelectionDto data)
{
    PlayerManager.Instance.HandleLoginSuccess(data);
}
```

#### **B) ShipSelectionUI Updates**

```csharp
// ESKİ
private void OnShipSelected(ShipSummaryDto ship)
{
    GameManager.Instance.SetActiveShipAndEnterGame(ship);
}

// YENİ
private void OnShipSelected(ShipSummaryDto ship)
{
    PlayerManager.Instance.HandleShipSelection(ship);
}
```

#### **C) AttackButton/Combat UI Updates**

```csharp
// ESKİ - Direct ProjectileManager calls
public void OnAttackButtonPressed()
{
    var prefab = GetCannonballPrefab();
    ProjectileManager.Instance.SpawnProjectile(prefab, ...);
}

// YENİ - CombatManager integration
public void OnAttackButtonPressed()
{
    bool success = CombatManager.Instance.FireActiveCannonball();
    if (success)
    {
        ShowAttackFeedback();
    }
}
```

---

## 🧪 Migration Testing Checklist

### **Phase 1 Testing: Core Systems**
- [ ] Bootstrap sahnesinde SystemCoordinator çalışıyor
- [ ] Login → PlayerManager.HandleLoginSuccess() çağrılıyor
- [ ] Ship Selection → PlayerManager.HandleShipSelection() çağrılıyor
- [ ] Scene transitions SceneController üzerinden
- [ ] Player data persistency çalışıyor

### **Phase 2 Testing: Combat Systems**
- [ ] PrefabManager.asset oluşturuldu ve dolduruldu
- [ ] ProjectileManager PrefabManager entegrasyonu
- [ ] CombatManager target selection çalışıyor
- [ ] Cannonball firing test edildi
- [ ] Combat range ve auto-targeting test edildi

### **Phase 3 Testing: Scene Management**
- [ ] Bootstrap → Login geçişi otomatik
- [ ] Login → Ship Selection → Game flow
- [ ] Loading screen'ler çalışıyor
- [ ] Scene-specific sistem initialization

### **Phase 4 Testing: UI Systems**
- [ ] Login UI yeni sistemle entegre
- [ ] Ship Selection UI güncellenmiş
- [ ] Attack button CombatManager kullanıyor
- [ ] Debug UI'lar (F1/F2) çalışıyor

---

## 🔧 Asset Configuration Migration

### **GameSettings Asset Creation**
```
1. Mevcut game settings'leri not alın
2. Resources/GameSettings.asset oluşturun
3. Değerleri transfer edin:
   - Projectile settings
   - Combat settings
   - Audio settings
```

### **PrefabManager Asset Setup**
```
1. Mevcut prefab referanslarını listeleyin:
   - Cannonball prefabs (CB1, CB2, Shrapnel)
   - Effect prefabs (Hit, Explosion)
   - Network prefabs (Player, Ship)

2. Resources/PrefabManager.asset oluşturun
3. Tüm prefab'ları categorize ederek atayın
4. ID'leri API database ile sync edin
```

---

## 🚨 Migration Risk Management

### **Yüksek Risk Alanları**
1. **Network integration** - NetworkManager sahne-specific
2. **Save/Load systems** - Player data format değişiklikleri
3. **API calls** - Endpoint mappings
4. **Third-party integrations** - Mirror, WebSocket
5. **Performance** - Sistem başlatma süreleri

### **Risk Mitigation**
```
1. BACKUP: Migration öncesi full project backup
2. TESTING: Her phase sonrası full testing
3. ROLLBACK: Eski sistemleri hemen silmeyin
4. GRADUAL: Bir sahne, bir sistem migration
5. VALIDATION: Sürekli debug tools kullanın
```

### **Rollback Plan**
```
Migration başarısız olursa:
1. Git commit'lere geri dön
2. Eski GameManager'ı reactivate et
3. SystemCoordinator'ı disable et
4. Scene'lerdeki yeni sistemleri disable et
5. Test ederek eski flow'u restore et
```

---

## 📈 Migration Success Metrics

### **Performance Targets**
- Sistem başlatma süresi < 2 saniye
- Scene geçiş süresi < 3 saniye
- Memory usage stable (no leaks)
- 60 FPS maintenance

### **Quality Targets**
- Zero console errors
- All debug tools functional
- Complete feature parity
- Improved maintainability
- Better debugging capabilities

---

## 🎯 Post-Migration Cleanup

### **Code Cleanup**
1. Eski GameManager'ı sil (tamamen test edildikten sonra)
2. Unused using statements temizle
3. Deprecated method'ları kaldır
4. Code documentation güncelle

### **Performance Optimization**
1. Profiler ile system startup optimize et
2. Memory allocation patterns kontrol et
3. Unnecessary singleton creations minimize et
4. Event subscriptions leak kontrolü

### **Documentation Updates**
1. Code comments güncelle
2. System interaction diagrams
3. New developer onboarding guide
4. Troubleshooting documentation

**Migration tamamlandığında clean, maintainable ve scalable bir architecture'a sahip olacaksınız! 🚀** 