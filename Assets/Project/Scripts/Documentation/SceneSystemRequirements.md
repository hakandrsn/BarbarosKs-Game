# 🎬 Sahne-Bazlı Sistem Gereksinimleri

Bu döküman her sahnenin hangi sistemlere ihtiyaç duyduğunu ve hangi sistemlerin aktif olması gerektiğini belirtir.

## 🚀 Bootstrap Sahnesinde Olması Gerekenler

### **Zorunlu Core Sistemler**
- **SystemCoordinator** - Tüm sistemlerin başlatılması için
- **SceneController** - Sahne yönetimi için
- **GameStateManager** - Bootstrap state management
- **GameSettings** (Asset) - Temel game ayarları
- **PrefabManager** (Asset) - Prefab referansları

### **Zorunlu Data Sistemleri**
- **PlayerManager** - Player data initialization
- **GameDataManager** - Game data structures
- **DataInitializer** - Data loading coordination

### **Network & API**
- **ApiManager** - API bağlantısı
- **CannonballService** - API servisleri

### **Bootstrap Sequence**
1. SystemCoordinator tüm sistemleri başlatır
2. 1-2 saniye bekler (sistem initialization)
3. Otomatik Login sahnesine yönlendirir

### **Bootstrap'te OLMAYANLAR**
- UI sistemleri (minimal)
- Combat sistemleri
- Audio sistemleri

---

## 🔐 Login Sahnesinde Olması Gerekenler

### **Core Sistemler**
- **SceneController** - Sahne yönetimi
- **GameStateManager** - Login state
- **PlayerManager** - Login sonrası data handling

### **Network & API**
- **ApiManager** - Login API calls
- **NetworkManager** - ❌ HENÜZ DEĞİL (login tamamlandıktan sonra)

### **UI Sistemleri**
- **LoginUI** (scene-specific)
- **LoadingScreen** - Login loading states

### **Login Flow**
1. User credentials → ApiManager → Login API
2. Success → PlayerManager.HandleLoginSuccess()
3. SceneController → Ship selection or Create ship

### **Login'de OLMAYANLAR**
- Combat sistemleri
- Market sistemleri
- Gameplay sistemleri

---

## 📝 Register Sahnesinde Olması Gerekenler

### **Core Sistemler**
- **SceneController** - Sahne yönetimi
- **GameStateManager** - Register state
- **PlayerManager** - Register sonrası data handling

### **Network & API**
- **ApiManager** - Register API calls

### **UI Sistemleri**
- **RegisterUI** (scene-specific)
- **LoadingScreen** - Register loading states

### **Register Flow**
1. User data → ApiManager → Register API
2. Success → PlayerManager.HandleLoginSuccess()
3. SceneController → Create ship (first time user)

---

## ⏳ Loading Sahnesinde Olması Gerekenler

### **Core Sistemler**
- **LoadingManager** - Loading orchestration
- **SceneController** - Background scene loading

### **UI Sistemleri**
- **LoadingScreen** - Progress visualization

### **Loading Features**
- Progress tracking
- Minimum loading time
- Error handling
- Background scene loading

### **Loading'de OLMAYANLAR**
- Gameplay sistemleri
- Heavy data processing
- Network operations (background only)

---

## 🚢 SelectShipScene'de Olması Gerekenler

### **Core Sistemler**
- **SceneController** - Sahne yönetimi
- **GameStateManager** - Ship selection state
- **PlayerManager** - Ship data ve selection handling
- **GameDataManager** - Ship data visualization

### **Data Sistemleri**
- **PlayerManager** - Owned ships listesi
- Player'ın ship listesi loaded olmalı

### **UI Sistemleri**
- **ShipSelectionUI** (scene-specific)
- Ship preview sistemleri

### **Ship Selection Flow**
1. PlayerManager.OwnedShips display edilir
2. User selection → PlayerManager.HandleShipSelection()
3. SceneController → Game scene

### **SelectShip'te OLMAYANLAR**
- Combat sistemleri
- Market sistemleri (ship satın alma başka sahnede)
- Network gameplay

---

## 🔨 CreateShip Sahnesinde Olması Gerekenler

### **Core Sistemler**
- **SceneController** - Sahne yönetimi  
- **GameStateManager** - Create ship state
- **PlayerManager** - New ship data handling

### **Network & API**
- **ApiManager** - Ship creation API
- Ship creation API endpoints

### **UI Sistemleri**
- **CreateShipUI** (scene-specific)
- Ship customization UI

### **Create Ship Flow**
1. Ship customization → API call
2. Success → PlayerManager update
3. SceneController → Ship selection

---

## 🎮 FisherSea (Game Scene) Olması Gerekenler

### **Core Sistemler (Hepsi Aktif)**
- **SystemCoordinator** - ✅ Active
- **SceneController** - ✅ Active  
- **GameStateManager** - InGame state
- **PlayerManager** - Game mode aktif
- **GameDataManager** - ✅ Full data loaded
- **GameSettings** - ✅ Game configuration
- **PrefabManager** - ✅ All prefabs ready

### **Gameplay Sistemleri**
- **CombatManager** - ✅ Combat orchestration
- **ProjectileManager** - ✅ Projectile spawning
- **WeaponSystem** - Player weapons
- **PlayerController** - Player movement & controls
- **ShipController** - Ship behaviors

### **Network Sistemleri**
- **NetworkManager** - ✅ Multiplayer networking
- **NetworkObjectSpawner** - Network object management
- **NetworkIdentity** - Player network identity

### **Audio & Effects**
- **AudioManager** - Game sounds
- Hit effects, explosion effects
- Dynamic audio management

### **UI Sistemleri**
- **GameUI** - In-game HUD
- **AttackButtonController** - Combat UI
- **PlayerInfoDisplay** - Player stats
- **TargetMarker** - Target visualization

### **Game Initialization Sequence**
1. SystemCoordinator → All systems ready
2. PlayerManager.EnterGame() → Game mode aktif
3. NetworkManager → Multiplayer connection
4. CombatManager → Combat systems ready
5. GameStateManager → InGame state

### **Game Scene Performance Requirements**
- All managers loaded and ready
- Network latency < 100ms
- 60 FPS target
- Memory usage optimized

---

## 🔄 Loading Screen (Between Scenes)

### **Core Sistemler**
- **LoadingManager** - Loading orchestration
- **SceneController** - Scene transition

### **Loading Types**
1. **Initial Loading** (Bootstrap → Login)
2. **Data Loading** (Login → Ship Selection)  
3. **Game Loading** (Ship Selection → Game)
4. **Background Loading** (During gameplay)

### **Loading Requirements**
- Progress indication
- Error handling
- Fallback mechanisms
- User feedback

---

## ⚙️ Sistem Prioritesi ve Yükleme Sırası

### **Priority 1: Core Systems**
1. SystemCoordinator
2. SceneController  
3. GameStateManager
4. GameSettings (Asset)
5. PrefabManager (Asset)

### **Priority 2: Data Systems**
1. PlayerManager
2. GameDataManager
3. DataInitializer

### **Priority 3: Network Systems**
1. ApiManager
2. CannonballService
3. NetworkManager (game scene'de)

### **Priority 4: Gameplay Systems**
1. ProjectileManager
2. CombatManager
3. AudioManager

### **Priority 5: UI Systems**
1. LoadingManager
2. MarketManager (market sahnelerinde)
3. Scene-specific UI components

---

## 🔧 Konfigürasyon Gereksinimleri

### **Assets (Resources Klasöründe Olmalı)**
```
Resources/
├── GameSettings.asset
├── PrefabManager.asset
└── (Game configuration assets)
```

### **Prefab Assignments (PrefabManager.asset)**
```
Cannonball Prefabs:
├── CB1 → CB1.prefab
├── CB2 → CB2.prefab  
├── Shrapnel → Shrapnel.prefab
└── (Other cannonball types)

Effect Prefabs:
├── HitEffect → HitEffect.prefab
├── ExplosionEffect → ExplosionEffect.prefab
└── Lightning → Lightning.prefab

Network Prefabs:
├── Player → PlayerPrefab.prefab
└── NetworkShip → NetworkShip.prefab
```

### **Network Configuration**
- API Base URL configuration
- Network timeout settings
- Retry mechanisms

---

## 📊 Sistem Durumu Monitoring

### **Debug Tools**
- SystemCoordinator.ValidateAllSystems()
- PlayerManager.DebugShowPlayerInfo()
- CombatManager.DebugShowCombatInfo()
- F1: Quick system check
- F2: Data loading trigger

### **Health Checks**
- All singleton instances active
- Network connectivity
- API responsiveness
- Data loading status
- Memory usage monitoring

---

## 🚨 Error Handling ve Fallbacks

### **Missing System Handling**
- SystemCoordinator auto-creates missing systems
- Fallback prefabs için alternative mechanisms
- Network failure fallbacks
- Data loading error recovery

### **Scene Transition Failures**
- SceneController error handling
- Loading screen timeout handling
- Recovery mechanisms

Bu dokümantasyon Unity projenizin her sahnesinde hangi sistemlerin aktif olması gerektiğini net bir şekilde belirtir. Yeni bir sahne oluştururken veya mevcut sahneleri debug ederken bu rehberi kullanabilirsiniz. 