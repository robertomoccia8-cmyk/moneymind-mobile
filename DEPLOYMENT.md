# Deployment Guide - MoneyMindApp

## 🎯 Deployment Targets

1. **Google Play** (Android) - PRIORITÀ
2. **Apple App Store** (iOS) - Opzionale (richiede Mac)
3. **Microsoft Store** (Windows) - Bonus

---

## 📦 Pre-Deployment Checklist

- [ ] Versione aggiornata in `VersionManager.cs`
- [ ] Changelog compilato in `CHANGELOG.md`
- [ ] Tutti i test passano (`dotnet test`)
- [ ] Crash-free rate > 99.5% in beta
- [ ] Privacy Policy aggiornata
- [ ] Screenshots/video pronti (5 lingue se multi-language)
- [ ] Firma APK/AAB con keystore production
- [ ] Beta testers feedback indirizzato
- [ ] Permissions AndroidManifest/Info.plist verificate

---

## 🤖 Android - Google Play

### 1. Preparazione Build

**Versioning** (`MoneyMindApp.csproj`):
```xml
<PropertyGroup>
    <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion> <!-- Incrementa ogni release -->
</PropertyGroup>
```

**Signing Config**:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>C:\Keys\moneymind-release.keystore</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>moneymind</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>$(KEYSTORE_PASSWORD)</AndroidSigningKeyPass>
    <AndroidSigningStorePass>$(KEYSTORE_PASSWORD)</AndroidSigningStorePass>
</PropertyGroup>
```

**Genera Keystore** (una volta sola, SALVA IN SICUREZZA!):
```bash
keytool -genkey -v -keystore moneymind-release.keystore -alias moneymind \
        -keyalg RSA -keysize 2048 -validity 10000
```

**⚠️ CRITICO**: Backup keystore + password in 3 posti sicuri (perso = mai più aggiornare app!).

### 2. Build Release (AAB)

```bash
dotnet publish MoneyMindApp.csproj \
    -f net8.0-android \
    -c Release \
    -p:AndroidPackageFormat=aab \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore=moneymind-release.keystore \
    -p:AndroidSigningKeyAlias=moneymind \
    -p:AndroidSigningKeyPass=%KEYSTORE_PASSWORD% \
    -p:AndroidSigningStorePass=%KEYSTORE_PASSWORD%
```

**Output**: `bin/Release/net8.0-android/publish/com.moneymind.app-Signed.aab`

**Verifica AAB**:
```bash
# Lista contenuto
unzip -l com.moneymind.app-Signed.aab

# Verifica firma
jarsigner -verify -verbose -certs com.moneymind.app-Signed.aab
```

### 3. Google Play Console Setup

**URL**: https://play.google.com/console

#### A. Crea App
1. **Create App** → Nome "MoneyMind", Default language "Italian"
2. **Category**: Finance
3. **Type**: App
4. **Free/Paid**: Free (con beta license in-app)

#### B. Store Listing

**App Name**: MoneyMind - Finanze Personali
**Short Description** (80 chars):
```
Gestisci le tue finanze con grafici, import bancari e sync WiFi. Dati in locale.
```

**Full Description** (4000 chars):
```
MoneyMind è l'app completa per gestire le tue finanze personali in modo semplice e sicuro.

🌟 CARATTERISTICHE PRINCIPALI
• Dashboard con statistiche in tempo reale (saldo, entrate, uscite, risparmio)
• Importa estratti conto CSV/Excel dalla tua banca
• Grafici dettagliati mensili e annuali
• Gestione multi-conto (conti correnti illimitati)
• Sincronizzazione WiFi con app desktop (dati sempre in locale!)
• Rileva duplicati automaticamente
• Configura periodi stipendiali personalizzati
• Esporta dati in Excel, CSV, PDF

🔒 PRIVACY & SICUREZZA
• Dati 100% in locale, MAI caricati su cloud
• Blocco app con Face ID / Touch ID
• Encryption database opzionale
• Zero tracking, zero pubblicità

📊 ANALISI AVANZATE
• Grafici entrate/uscite mensili
• Confronto anni
• Trend risparmio
• Statistiche periodo stipendiale

💰 PERIODI STIPENDIALI
• Calcola statistiche da stipendio a stipendio (non mese calendario)
• Gestione weekend intelligente
• Eccezioni per mesi specifici

🔄 SINCRONIZZAZIONE DESKTOP
• WiFi Sync: sincronizza con app desktop via rete locale
• File Export: esporta/importa file .mmsync
• Hotspot supportato!

💎 IMPORT INTELLIGENTE
• CSV, Excel supportati
• Mapping colonne automatico
• Configurazioni salvate per riutilizzo
• Import bulk 1000+ transazioni in secondi

🎨 DESIGN MODERNO
• Material Design 3
• Dark theme
• Animazioni fluide
• UI intuitiva

🆓 BETA GRATUITA
MoneyMind è attualmente in beta. Richiedi una beta key gratuita!

🔗 LINK UTILI
• GitHub: https://github.com/[username]/moneymind
• Support: support@moneymind.app
• Privacy Policy: [URL]

⭐ Provala ora e prendi il controllo delle tue finanze!
```

**Screenshots** (min 2, max 8):
- Dashboard (stats cards)
- Transactions list (con swipe)
- Import CSV wizard
- Analytics charts
- Dark theme
- WiFi Sync
- Multi-account selection
- Biometric lock

**Format**: JPG/PNG, 16:9 aspect ratio, 1024x500 px minimum

**Feature Graphic**: 1024x500 px (obbligatorio)

**App Icon**: 512x512 px PNG (già in `Resources/Images/appicon.png`)

#### C. Content Rating

**Questionnaire**:
- App contains ads? **NO**
- App allows user interaction? **NO** (no social features)
- App shares user location? **NO**
- App collects personal data? **YES** (email for beta license)

**Rating**: Everyone / PEGI 3

#### D. App Content

**Privacy Policy**: [URL - host su GitHub Pages o sito]

**Ads**: NO

**In-App Purchases**: NO (beta license gratuita)

**Target Audience**: Age 18+

**COVID-19 Contact Tracing**: NO

#### E. Pricing & Distribution

**Countries**: Worldwide (o seleziona paesi specifici)

**Price**: Free

**Device Categories**: Phone, Tablet

#### F. App Releases

**Internal Testing** (10-20 testers):
- Upload AAB
- Add tester emails
- Share opt-in link

**Closed Testing** (100 testers):
- Invite-only
- Feedback loop

**Open Testing** (5000 testers):
- Public opt-in URL
- Final stress test

**Production**:
- **Gradual Rollout**: 10% → 25% → 50% → 100% (su 7 giorni)
- Monitor crash rate
- Pause rollout se crash > 1%

### 4. Update Workflow

**Incrementa Versione**:
```xml
<ApplicationVersion>2</ApplicationVersion> <!-- Era 1 -->
<ApplicationDisplayVersion>1.0.1</ApplicationDisplayVersion>
```

**Build AAB**:
```bash
dotnet publish ... # Stesso comando
```

**Upload to Play Console**:
- Release Management → Production → Create New Release
- Upload AAB
- Release Notes (cosa c'è di nuovo)
- Review → Rollout to Production

**Google Review Time**: 1-3 giorni (più veloce dopo prime releases).

---

## 🍎 iOS - App Store (Opzionale)

**Requisiti**:
- Mac con Xcode 15+
- Apple Developer Program ($99/anno)
- Provisioning Profile + Certificate

### 1. Build iOS

```bash
dotnet publish MoneyMindApp.csproj \
    -f net8.0-ios \
    -c Release \
    -p:ArchiveOnBuild=true \
    -p:RuntimeIdentifier=ios-arm64
```

**Output**: `bin/Release/net8.0-ios/ios-arm64/MoneyMindApp.ipa`

### 2. App Store Connect

**URL**: https://appstoreconnect.apple.com

**Setup simile a Google Play**:
- Create App
- Fill metadata (Name, Description, Keywords, Screenshots)
- Privacy Policy URL
- Age Rating
- App Review Information
- Upload IPA via Xcode Organizer o Transporter

**Review Time**: 24-48 ore (più rigoroso di Google).

**Gotchas**:
- Richiede spiegazione dettagliata ogni permission (Info.plist)
- Reviewer testa app manualmente
- Possibile rejection se UI confusa

---

## 🪟 Windows - Microsoft Store (Opzionale)

### Build MSIX

```bash
dotnet publish MoneyMindApp.csproj \
    -f net8.0-windows10.0.19041.0 \
    -c Release \
    -p:GenerateAppxPackageOnBuild=true \
    -p:AppxPackageSigningEnabled=true
```

**Upload su Partner Center**:
https://partner.microsoft.com/dashboard

**Nota**: Windows mobile praticamente morto, priorità bassa.

---

## 📊 Post-Release Monitoring

### Metrics to Track

**Google Play Console**:
- **Crash-free sessions**: Target > 99.5%
- **ANR (App Not Responding)**: Target < 0.1%
- **Ratings**: Target > 4.5 ⭐
- **Installations**: Daily active users (DAU)
- **Uninstalls**: Retention rate

**Firebase Crashlytics** (se implementato):
- Top crashes
- Affected users
- OS versions impacted

**User Reviews**:
- Respond to 1-star reviews in 24h
- Thank 5-star reviews
- Address bugs mentioned frequently

### Rollback Strategy

Se crash rate > 2% dopo 24h:
1. **Pause Rollout** in Play Console
2. Investigate crash logs
3. Hotfix critical bug
4. Test rigorosamente
5. Release v1.0.1 (hotfix)
6. Resume rollout graduale

---

## 🔄 CI/CD Automation (GitHub Actions)

**`.github/workflows/deploy-android.yml`**:

```yaml
name: Deploy Android to Google Play

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  build-and-deploy:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x

      - name: Restore Dependencies
        run: dotnet restore

      - name: Build AAB
        run: dotnet publish MoneyMindApp.csproj -f net8.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true ...
        env:
          KEYSTORE_PASSWORD: ${{ secrets.KEYSTORE_PASSWORD }}

      - name: Upload to Google Play (Internal Testing)
        uses: r0adkll/upload-google-play@v1
        with:
          serviceAccountJsonPlainText: ${{ secrets.GOOGLE_SERVICE_ACCOUNT_JSON }}
          packageName: com.moneymind.app
          releaseFiles: bin/Release/net8.0-android/publish/*.aab
          track: internal
          status: completed
```

**Secrets da Configurare**:
- `KEYSTORE_PASSWORD`
- `GOOGLE_SERVICE_ACCOUNT_JSON` (da Google Play Console → API Access)

---

## 📝 Release Notes Template

**v1.0.0 (Initial Release)**:
```
🎉 Prima release di MoneyMindApp!

✨ Novità:
• Dashboard con statistiche in tempo reale
• Importa estratti conto CSV/Excel
• Grafici mensili e annuali
• Sincronizzazione WiFi con desktop
• Gestione multi-conto
• Blocco app con Face ID / Touch ID

🔒 Privacy & Sicurezza:
• Dati 100% in locale, mai su cloud
• Encryption database opzionale
• Zero tracking

📱 Compatibilità:
• Android 7.0+ (API 24)
• Ottimizzato per Android 13/14

🐛 Bug Fix:
• N/A (prima release)

📧 Feedback: support@moneymind.app
```

**v1.0.1 (Bugfix)**:
```
🔧 Correzioni:
• Fix crash su import CSV > 1000 righe
• Fix calcolo saldo con transazioni future
• Migliorata performance lista transazioni

🎨 Miglioramenti UI:
• Dark theme più contrastato
• Animazioni più fluide

📱 Compatibilità:
• Fix compatibilità Android 14

Grazie a tutti i beta testers per il feedback!
```

---

## 🚨 App Rejection - Common Issues

### Google Play

**Reason**: App crasha al launch
**Fix**: Test su device pulito, fix crash, resubmit

**Reason**: Privacy Policy link broken
**Fix**: Verifica URL accessibile, update listing

**Reason**: Permissions non giustificate
**Fix**: Aggiungi rationale in AndroidManifest per ogni permission

### App Store

**Reason**: "App is confusing"
**Fix**: Miglior onboarding, tutorial, help text

**Reason**: Missing privacy description
**Fix**: Aggiungi NSUsageDescription per ogni permission in Info.plist

**Reason**: "Cannot test beta features"
**Fix**: Fornisci test account al reviewer con beta key valida

---

## 📱 Beta Testing Opt-In Links

**Google Play (Internal)**:
```
https://play.google.com/apps/internaltest/XXXXXXX
```

**Google Play (Open Beta)**:
```
https://play.google.com/store/apps/details?id=com.moneymind.app
```

**TestFlight (iOS)**:
```
https://testflight.apple.com/join/XXXXXXX
```

---

## 🎯 Launch Checklist

**-7 giorni**:
- [ ] AAB/IPA build pronti
- [ ] Store listings completi
- [ ] Screenshots finali
- [ ] Privacy Policy online
- [ ] Beta testers notificati

**-3 giorni**:
- [ ] Submit to stores
- [ ] Prepare social media posts
- [ ] Email list beta testers

**Day 0 (Launch)**:
- [ ] Gradual rollout 10%
- [ ] Monitor crash rate ogni 2h
- [ ] Respond to first reviews

**Day 1-7**:
- [ ] Increase rollout 25% → 50% → 100%
- [ ] Fix critical bugs entro 24h
- [ ] Celebrate! 🎉

---

**Ultima Review**: 2025-01-XX
