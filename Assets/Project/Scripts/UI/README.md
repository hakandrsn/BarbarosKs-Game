# 🔫 Attack Button System - Otomatik Saldırı Sistemi

Bu sistem oyunculara **otomatik saldırı** özelliği sağlar. Space tuşu ile manuel ateş etmenin yanı sıra, UI butonu ile sürekli otomatik ateş yapabilirsiniz.

## ✨ Özellikler

### 🎯 3 Farklı Buton Durumu:
1. **🟢 Saldırabilir**: Hedef seçili ve menzilde → Otomatik ateş başlatabilir
2. **🔴 Saldırıyor**: Şu anda otomatik ateş ediyor → Durdurmak için tıklayın  
3. **⚫ Pasif**: Hedef yok veya menzil dışı → Buton devre dışı

### ⚙️ Akıllı Sistem:
- **Otomatik Durdurma**: Hedef menzilden çıkarsa veya ölürse auto-attack durur
- **Server Sync**: Attack cooldown sunucudan gelir, hileli hızlandırma önlenir
- **Space Uyumlu**: Space tuşu ile buton aynı sistemi kullanır

## 🚀 Kurulum

### Adım 1: UI Setup Script'ini Ekleyin
```csharp
// Herhangi bir GameObject'e AttackButtonSetup component'ini ekleyin
// Otomatik olarak UI'ı oluşturacak
```

### Adım 2: Sahneye Setup Script'ini Ekleyin
1. Unity Editor'da FisherSea sahnesini açın
2. Boş bir GameObject oluşturun: `GameObject → Create Empty`
3. İsim verin: `"UI Manager"`
4. **AttackButtonSetup** component'ini ekleyin
5. Inspector'da **Auto Setup On Start** işaretli olduğundan emin olun

### Adım 3: Oyunu Test Edin
- Oyunu başlattığınızda otomatik olarak sağ alt köşede attack butonu oluşacak
- Bir düşman seçin (tıklayın)
- Attack butonuna tıklayın → Otomatik ateş başlar
- Tekrar tıklayın → Durur

## 🎮 Kullanım

### Manual Kurulum (İsteğe Bağlı)
Eğer otomatik kurulum çalışmazsa:

1. **Canvas Oluştur**:
   ```
   Hierarchy → UI → Canvas
   ```

2. **Attack Button Ekle**:
   ```
   Canvas → UI → Button - TextMeshPro
   ```

3. **Component Ekle**:
   ```
   Button → Add Component → AttackButtonController
   ```

4. **Referansları Bağla**:
   - Attack Button: Button component'ini sürükle
   - Button Text: Text component'ini sürükle  
   - Button Icon: Image component'ini sürükle

## 🔧 Özelleştirme

### AttackButtonController Ayarları:
```csharp
[Header("Button Colors")]
public Color canAttackColor = Color.green;    // Saldırabilir rengi
public Color attackingColor = Color.red;      // Saldırıyor rengi  
public Color disabledColor = Color.gray;      // Pasif rengi

[Header("Auto Attack Settings")]
public bool enableAutoAttack = true;          // Otomatik ateş aktif mi?
```

### Public Metodlar:
```csharp
// Otomatik ateş durumunu kontrol et
bool isAutoAttacking = controller.IsAutoAttacking();

// Otomatik ateş aç/kapat
controller.SetAutoAttackEnabled(false);

// Attack cooldown güncelle (Network'ten gelir)
controller.UpdateAttackCooldown(3.0f);

// Buton durumunu öğren
AttackButtonState state = controller.GetCurrentState();
```

## 🌐 Network Entegrasyonu

Sistem otomatik olarak network ile entegre:

1. **Attack Request**: Buton sunucuya ateş isteği gönderir
2. **Server Validation**: Sunucu menzil/cooldown kontrol eder
3. **Response**: Onay gelirse ateş efekti çalar
4. **Cooldown Update**: Sunucu yeni cooldown süresini gönderir

## 🐛 Sorun Giderme

### "AttackButtonController bulunamadı" Hatası:
```bash
# AttackButtonSetup component'inin autoSetupOnStart = true olduğundan emin olun
# Veya Context Menu'den "Setup Attack Button UI" çalıştırın
```

### "Local Player bulunamadı" Hatası:
```bash
# PlayerController'ın isLocalPlayer = true olduğundan emin olun
# Network connection'ının çalıştığından emin olun
```

### UI Görünmüyor:
```bash
# Canvas'ın Render Mode = Screen Space - Overlay olduğundan emin olun
# Canvas'ın Sorting Order > 0 olduğundan emin olun
```

## 📱 UI Pozisyonu

Varsayılan pozisyon: **Sağ alt köşe (120px içeride)**

Değiştirmek için:
```csharp
// AttackButtonSetup.cs → CreateAttackButton() methodunda:
buttonRect.anchoredPosition = new Vector2(-120f, 120f); // X, Y pozisyon
buttonRect.sizeDelta = new Vector2(100f, 100f);         // Genişlik, Yükseklik
```

## ⚡ Performans

- **Optimize**: Update döngüsü sadece local player için çalışır
- **Event-Driven**: Network mesajları event-based
- **Cache**: PlayerController ve target referansları cache'lenir
- **Minimal GC**: Object pooling kullanılır

---

**🎯 Ready to Fight!** Artık hem Space tuşu hem de UI butonu ile otomatik saldırı yapabilirsiniz! 