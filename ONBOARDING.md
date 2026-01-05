# Onboarding Flow - First Time User Experience

## 🎯 Obiettivo

Guidare nuovo utente attraverso setup iniziale in **< 2 minuti** con UX moderna e chiara.

**Principio**: *"Show, Don't Tell"* - Minimo testo, massima interattività.

---

## 📊 Onboarding Flow (5 Steps)

```
App Launch
    ↓
[Check First Launch]
    ├─ NO → MainPage (utente esistente)
    └─ YES ↓

Step 1: Welcome Screen
    ↓
Step 2: Beta License Activation
    ↓
Step 3: Create First Account
    ↓
Step 4: Biometric Setup (Optional)
    ↓
Step 5: Quick Tour (Optional, Skippable)
    ↓
MainPage (Dashboard)
```

---

## 🏁 Step 1: Welcome Screen

### UI

**File**: `Views/Onboarding/WelcomeP

age.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MoneyMindApp.Views.Onboarding.WelcomePage"
             NavigationPage.HasNavigationBar="False"
             BackgroundColor="#6750A4">

    <Grid RowDefinitions="*,Auto,Auto,Auto" Padding="40,60,40,40">

        <!-- Logo + Animation -->
        <VerticalStackLayout Grid.Row="0" VerticalOptions="Center" Spacing="20">
            <Image Source="logo_white.png" WidthRequest="120" HeightRequest="120"
                   HorizontalOptions="Center">
                <Image.Behaviors>
                    <!-- Fade in + scale animation -->
                </Image.Behaviors>
            </Image>

            <Label Text="MoneyMind" FontSize="36" FontAttributes="Bold"
                   TextColor="White" HorizontalOptions="Center" />

            <Label Text="Il tuo assistente finanziario personale" FontSize="16"
                   TextColor="#E0E0E0" HorizontalOptions="Center"
                   HorizontalTextAlignment="Center" />
        </VerticalStackLayout>

        <!-- Features List -->
        <VerticalStackLayout Grid.Row="1" Spacing="16" Margin="0,0,0,40">
            <HorizontalStackLayout Spacing="12">
                <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                <Label Text="Gestisci tutte le tue transazioni" FontSize="14"
                       TextColor="White" VerticalOptions="Center" />
            </HorizontalStackLayout>

            <HorizontalStackLayout Spacing="12">
                <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                <Label Text="Importa estratti conto bancari" FontSize="14"
                       TextColor="White" VerticalOptions="Center" />
            </HorizontalStackLayout>

            <HorizontalStackLayout Spacing="12">
                <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                <Label Text="Analizza le tue finanze con grafici" FontSize="14"
                       TextColor="White" VerticalOptions="Center" />
            </HorizontalStackLayout>

            <HorizontalStackLayout Spacing="12">
                <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                <Label Text="Dati 100% in locale, mai su cloud" FontSize="14"
                       TextColor="White" VerticalOptions="Center" />
            </HorizontalStackLayout>
        </VerticalStackLayout>

        <!-- CTA Button -->
        <Button Grid.Row="2" Text="Inizia" FontSize="18" FontAttributes="Bold"
                BackgroundColor="White" TextColor="#6750A4"
                HeightRequest="56" CornerRadius="28"
                Command="{Binding NextCommand}"
                HorizontalOptions="Fill" />

        <!-- Skip (per tester) -->
        <Button Grid.Row="3" Text="Salta Setup (Testing)"
                BackgroundColor="Transparent" TextColor="#E0E0E0"
                FontSize="12" Margin="0,10,0,0"
                Command="{Binding SkipCommand}"
                IsVisible="{Binding IsDebugMode}" />

    </Grid>

</ContentPage>
```

**Animation**: Logo fade-in + scale (0.8 → 1.0) in 600ms.

---

## 🔐 Step 2: Beta License Activation

### UI

**File**: `Views/Onboarding/LicenseActivationPage.xaml`

```xml
<ContentPage Title="Attiva Licenza Beta">
    <ScrollView Padding="20">
        <VerticalStackLayout Spacing="20">

            <!-- Header -->
            <VerticalStackLayout Spacing="8">
                <Label Text="🔑 Attivazione Beta" FontSize="24" FontAttributes="Bold" />
                <Label Text="MoneyMind è attualmente in fase beta. Inserisci la tua chiave beta per continuare."
                       FontSize="14" TextColor="Gray" LineHeight="1.4" />
            </VerticalStackLayout>

            <!-- Input Beta Key -->
            <Border Stroke="LightGray" StrokeThickness="1" Padding="0"
                    StrokeShape="RoundRectangle 12">
                <VerticalStackLayout Spacing="0">
                    <Label Text="Beta Key" FontSize="12" TextColor="Gray"
                           Margin="12,8,12,0" />
                    <Entry Placeholder="BETA-XXXX-XXXX-XXXX"
                           Text="{Binding BetaKey}"
                           FontSize="16" Margin="12,0,12,8"
                           ReturnCommand="{Binding ActivateCommand}" />
                </VerticalStackLayout>
            </Border>

            <!-- Input Email -->
            <Border Stroke="LightGray" StrokeThickness="1" Padding="0"
                    StrokeShape="RoundRectangle 12">
                <VerticalStackLayout Spacing="0">
                    <Label Text="Email" FontSize="12" TextColor="Gray"
                           Margin="12,8,12,0" />
                    <Entry Placeholder="tua@email.com"
                           Text="{Binding Email}"
                           Keyboard="Email" FontSize="16" Margin="12,0,12,8" />
                </VerticalStackLayout>
            </Border>

            <!-- Error Message -->
            <Label Text="{Binding ErrorMessage}" TextColor="Red"
                   IsVisible="{Binding HasError}" FontSize="14" />

            <!-- Activate Button -->
            <Button Text="Attiva Licenza" FontSize="16" FontAttributes="Bold"
                    BackgroundColor="#6750A4" TextColor="White"
                    HeightRequest="50" CornerRadius="25"
                    Command="{Binding ActivateCommand}"
                    IsEnabled="{Binding IsNotActivating}" />

            <!-- Loading -->
            <ActivityIndicator IsRunning="{Binding IsActivating}"
                               IsVisible="{Binding IsActivating}"
                               Color="#6750A4" />

            <!-- Help Link -->
            <Label HorizontalOptions="Center" Margin="0,20,0,0">
                <Label.FormattedText>
                    <FormattedString>
                        <Span Text="Non hai una beta key? " FontSize="12" TextColor="Gray" />
                        <Span Text="Richiedi Accesso" FontSize="12" TextColor="#6750A4"
                              FontAttributes="Bold" TextDecorations="Underline">
                            <Span.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding RequestAccessCommand}" />
                            </Span.GestureRecognizers>
                        </Span>
                    </FormattedString>
                </Label.FormattedText>
            </Label>

            <!-- Privacy -->
            <Label FontSize="10" TextColor="Gray" HorizontalTextAlignment="Center"
                   LineHeight="1.3" Margin="0,20,0,0">
                <Label.FormattedText>
                    <FormattedString>
                        <Span Text="Continuando, accetti i " />
                        <Span Text="Termini di Servizio" TextDecorations="Underline">
                            <Span.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding OpenTermsCommand}" />
                            </Span.GestureRecognizers>
                        </Span>
                        <Span Text=" e la " />
                        <Span Text="Privacy Policy" TextDecorations="Underline">
                            <Span.GestureRecognizers>
                                <TapGestureRecognizer Command="{Binding OpenPrivacyCommand}" />
                            </Span.GestureRecognizers>
                        </Span>
                    </FormattedString>
                </Label.FormattedText>
            </Label>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### Logic

```csharp
[RelayCommand]
private async Task ActivateAsync()
{
    if (string.IsNullOrWhiteSpace(BetaKey) || string.IsNullOrWhiteSpace(Email))
    {
        ErrorMessage = "Compila tutti i campi.";
        HasError = true;
        return;
    }

    try
    {
        IsActivating = true;
        HasError = false;

        var result = await _licenseService.ActivateLicenseAsync(BetaKey, Email);

        if (result.Success)
        {
            // Salva licenza
            await SecureStorage.SetAsync("license_token", result.Token);
            Preferences.Set("license_activated", true);

            // Vai a prossimo step
            await Shell.Current.GoToAsync("//onboarding/account");
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Licenza non valida.";
            HasError = true;
        }
    }
    catch (HttpRequestException)
    {
        ErrorMessage = "Errore di connessione. Verifica internet e riprova.";
        HasError = true;
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Errore: {ex.Message}";
        HasError = true;
    }
    finally
    {
        IsActivating = false;
    }
}
```

---

## 🏦 Step 3: Create First Account

### UI

**File**: `Views/Onboarding/CreateAccountPage.xaml`

```xml
<ContentPage Title="Crea il Tuo Primo Conto">
    <ScrollView Padding="20">
        <VerticalStackLayout Spacing="20">

            <!-- Header -->
            <VerticalStackLayout Spacing="8">
                <Label Text="🏦 Primo Conto" FontSize="24" FontAttributes="Bold" />
                <Label Text="Crea il tuo primo conto corrente per iniziare a tracciare le transazioni."
                       FontSize="14" TextColor="Gray" LineHeight="1.4" />
            </VerticalStackLayout>

            <!-- Nome Conto -->
            <Border Stroke="LightGray" StrokeThickness="1" Padding="0"
                    StrokeShape="RoundRectangle 12">
                <VerticalStackLayout Spacing="0">
                    <Label Text="Nome Conto" FontSize="12" TextColor="Gray"
                           Margin="12,8,12,0" />
                    <Entry Placeholder="es: Conto Principale, Unicredit"
                           Text="{Binding AccountName}"
                           FontSize="16" Margin="12,0,12,8" />
                </VerticalStackLayout>
            </Border>

            <!-- Saldo Iniziale -->
            <Border Stroke="LightGray" StrokeThickness="1" Padding="0"
                    StrokeShape="RoundRectangle 12">
                <VerticalStackLayout Spacing="0">
                    <Label Text="Saldo Iniziale (€)" FontSize="12" TextColor="Gray"
                           Margin="12,8,12,0" />
                    <Entry Placeholder="0.00"
                           Text="{Binding InitialBalance}"
                           Keyboard="Numeric" FontSize="16" Margin="12,0,12,8" />
                    <Label Text="Inserisci il saldo attuale del tuo conto. Puoi modificarlo dopo."
                           FontSize="10" TextColor="Gray" Margin="12,0,12,8" />
                </VerticalStackLayout>
            </Border>

            <!-- Icon Selector -->
            <VerticalStackLayout Spacing="8">
                <Label Text="Icona" FontSize="14" FontAttributes="Bold" />
                <CollectionView ItemsSource="{Binding AvailableIcons}"
                                SelectionMode="Single"
                                SelectedItem="{Binding SelectedIcon}">
                    <CollectionView.ItemsLayout>
                        <GridItemsLayout Orientation="Vertical" Span="6" />
                    </CollectionView.ItemsLayout>
                    <CollectionView.ItemTemplate>
                        <DataTemplate>
                            <Frame Padding="0" Margin="4" CornerRadius="12"
                                   BorderColor="{Binding IsSelected, Converter={StaticResource BoolToColorConverter}}"
                                   HasShadow="False" HeightRequest="60" WidthRequest="60">
                                <Label Text="{Binding Icon}" FontSize="32"
                                       HorizontalOptions="Center" VerticalOptions="Center" />
                            </Frame>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>
            </VerticalStackLayout>

            <!-- Color Picker -->
            <VerticalStackLayout Spacing="8">
                <Label Text="Colore" FontSize="14" FontAttributes="Bold" />
                <FlexLayout BindableLayout.ItemsSource="{Binding AvailableColors}"
                            Wrap="Wrap" JustifyContent="Start">
                    <BindableLayout.ItemTemplate>
                        <DataTemplate>
                            <Frame Padding="0" Margin="4" CornerRadius="20"
                                   BorderColor="{Binding IsSelected, Converter={StaticResource BoolToColorConverter}}"
                                   BackgroundColor="{Binding Color}"
                                   HasShadow="False" HeightRequest="40" WidthRequest="40">
                                <Frame.GestureRecognizers>
                                    <TapGestureRecognizer Command="{Binding SelectColorCommand}" />
                                </Frame.GestureRecognizers>
                            </Frame>
                        </DataTemplate>
                    </BindableLayout.ItemTemplate>
                </FlexLayout>
            </VerticalStackLayout>

            <!-- Preview -->
            <Frame BorderColor="LightGray" Padding="16" CornerRadius="12" Margin="0,10,0,0">
                <VerticalStackLayout Spacing="8">
                    <Label Text="Anteprima" FontSize="12" TextColor="Gray" />
                    <HorizontalStackLayout Spacing="12">
                        <Frame Padding="12" CornerRadius="12"
                               BackgroundColor="{Binding SelectedColor}"
                               HasShadow="False">
                            <Label Text="{Binding SelectedIcon}" FontSize="24" />
                        </Frame>
                        <VerticalStackLayout VerticalOptions="Center" Spacing="4">
                            <Label Text="{Binding AccountName}" FontSize="16" FontAttributes="Bold" />
                            <Label Text="{Binding InitialBalance, StringFormat='€{0:N2}'}"
                                   FontSize="14" TextColor="Gray" />
                        </VerticalStackLayout>
                    </HorizontalStackLayout>
                </VerticalStackLayout>
            </Frame>

            <!-- Create Button -->
            <Button Text="Crea Conto" FontSize="16" FontAttributes="Bold"
                    BackgroundColor="#6750A4" TextColor="White"
                    HeightRequest="50" CornerRadius="25"
                    Command="{Binding CreateAccountCommand}"
                    IsEnabled="{Binding CanCreateAccount}" />

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### Logic

```csharp
[RelayCommand(CanExecute = nameof(CanCreateAccount))]
private async Task CreateAccountAsync()
{
    try
    {
        var account = new BankAccount
        {
            Nome = AccountName,
            Icona = SelectedIcon,
            Colore = SelectedColor,
            SaldoIniziale = decimal.Parse(InitialBalance),
            DataCreazione = DateTime.Now
        };

        await _accountService.CreateAccountAsync(account);

        // Imposta come conto attivo
        Preferences.Set("active_account_id", account.Id);
        Preferences.Set("first_launch_completed", true);

        // Vai a prossimo step
        await Shell.Current.GoToAsync("//onboarding/biometric");
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
    }
}

private bool CanCreateAccount()
{
    return !string.IsNullOrWhiteSpace(AccountName) &&
           !string.IsNullOrWhiteSpace(InitialBalance) &&
           decimal.TryParse(InitialBalance, out _);
}
```

**Available Icons**: 💳 🏦 💰 💵 💶 💷 🏧 📊 💼 🎯 ⭐ 🔵

**Available Colors**: 12 colori Material Design (Primary, Success, Error, Info, etc.)

---

## 🔐 Step 4: Biometric Setup (Optional)

### UI

**File**: `Views/Onboarding/BiometricSetupPage.xaml`

```xml
<ContentPage Title="Protezione App">
    <Grid RowDefinitions="*,Auto" Padding="20">

        <!-- Content -->
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="30" VerticalOptions="Center">

                <!-- Icon -->
                <Image Source="face_id_icon.png" WidthRequest="120" HeightRequest="120"
                       HorizontalOptions="Center" />

                <!-- Title -->
                <VerticalStackLayout Spacing="8">
                    <Label Text="🔒 Proteggi i Tuoi Dati" FontSize="24" FontAttributes="Bold"
                           HorizontalTextAlignment="Center" />
                    <Label Text="Usa Face ID / Touch ID per bloccare l'app quando non in uso."
                           FontSize="14" TextColor="Gray" HorizontalTextAlignment="Center"
                           LineHeight="1.4" />
                </VerticalStackLayout>

                <!-- Benefits -->
                <VerticalStackLayout Spacing="16">
                    <HorizontalStackLayout Spacing="12">
                        <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                        <Label Text="Accesso rapido e sicuro" FontSize="14" VerticalOptions="Center" />
                    </HorizontalStackLayout>

                    <HorizontalStackLayout Spacing="12">
                        <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                        <Label Text="Nessuno può vedere i tuoi dati" FontSize="14" VerticalOptions="Center" />
                    </HorizontalStackLayout>

                    <HorizontalStackLayout Spacing="12">
                        <Label Text="✓" FontSize="24" TextColor="#4CAF50" />
                        <Label Text="Puoi disattivarlo in qualsiasi momento" FontSize="14" VerticalOptions="Center" />
                    </HorizontalStackLayout>
                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <!-- Buttons -->
        <VerticalStackLayout Grid.Row="1" Spacing="12">
            <Button Text="Abilita Face ID / Touch ID" FontSize="16" FontAttributes="Bold"
                    BackgroundColor="#6750A4" TextColor="White"
                    HeightRequest="50" CornerRadius="25"
                    Command="{Binding EnableBiometricCommand}" />

            <Button Text="Salta per Ora" FontSize="14"
                    BackgroundColor="Transparent" TextColor="Gray"
                    Command="{Binding SkipBiometricCommand}" />
        </VerticalStackLayout>

    </Grid>
</ContentPage>
```

### Logic

```csharp
[RelayCommand]
private async Task EnableBiometricAsync()
{
    try
    {
        var isAvailable = await _biometricService.IsAvailableAsync();

        if (!isAvailable)
        {
            await Shell.Current.DisplayAlert(
                "Biometric Non Disponibile",
                "Il tuo dispositivo non supporta Face ID / Touch ID o non è configurato.",
                "OK");

            await SkipBiometricAsync();
            return;
        }

        // Test auth
        var authenticated = await _biometricService.AuthenticateAsync("Verifica identità per abilitare protezione app");

        if (authenticated)
        {
            Preferences.Set("biometric_enabled", true);

            await Shell.Current.DisplayAlert(
                "✅ Abilitato!",
                "Face ID / Touch ID è ora attivo. L'app richiederà autenticazione all'apertura.",
                "OK");

            await GoToNextStep();
        }
        else
        {
            await Shell.Current.DisplayAlert(
                "Autenticazione Fallita",
                "Non è stato possibile verificare la tua identità.",
                "OK");
        }
    }
    catch (Exception ex)
    {
        await Shell.Current.DisplayAlert("Errore", ex.Message, "OK");
    }
}

[RelayCommand]
private async Task SkipBiometricAsync()
{
    Preferences.Set("biometric_enabled", false);
    await GoToNextStep();
}

private async Task GoToNextStep()
{
    // Ultimo step: quick tour o vai a main
    var showTour = await Shell.Current.DisplayAlert(
        "Tour Guidato",
        "Vuoi un tour rapido delle funzionalità principali?",
        "Sì",
        "No, Inizia Subito");

    if (showTour)
    {
        await Shell.Current.GoToAsync("//onboarding/tour");
    }
    else
    {
        await CompleteOnboardingAsync();
    }
}
```

---

## 📱 Step 5: Quick Tour (Optional, Skippable)

### UI - Carousel con 3-4 Slide

**File**: `Views/Onboarding/QuickTourPage.xaml`

```xml
<ContentPage Title="Tour Guidato">
    <Grid RowDefinitions="*,Auto">

        <!-- Carousel -->
        <CarouselView Grid.Row="0" ItemsSource="{Binding TourSlides}"
                      IndicatorView="indicatorView"
                      CurrentItem="{Binding CurrentSlide}">
            <CarouselView.ItemTemplate>
                <DataTemplate>
                    <Grid Padding="40" RowDefinitions="*,Auto,Auto">
                        <Image Grid.Row="0" Source="{Binding Image}"
                               Aspect="AspectFit" HeightRequest="300" />
                        <Label Grid.Row="1" Text="{Binding Title}"
                               FontSize="24" FontAttributes="Bold"
                               HorizontalTextAlignment="Center" Margin="0,20,0,10" />
                        <Label Grid.Row="2" Text="{Binding Description}"
                               FontSize="14" TextColor="Gray"
                               HorizontalTextAlignment="Center" LineHeight="1.4" />
                    </Grid>
                </DataTemplate>
            </CarouselView.ItemTemplate>
        </CarouselView>

        <!-- Indicator + Buttons -->
        <VerticalStackLayout Grid.Row="1" Padding="20" Spacing="20">
            <IndicatorView x:Name="indicatorView"
                           IndicatorColor="LightGray"
                           SelectedIndicatorColor="#6750A4"
                           HorizontalOptions="Center" />

            <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                <Button Grid.Column="0" Text="Salta"
                        BackgroundColor="Transparent" BorderColor="Gray"
                        BorderWidth="1" TextColor="Gray"
                        Command="{Binding SkipTourCommand}" />

                <Button Grid.Column="1" Text="{Binding NextButtonText}"
                        BackgroundColor="#6750A4" TextColor="White"
                        Command="{Binding NextCommand}" />
            </Grid>
        </VerticalStackLayout>

    </Grid>
</ContentPage>
```

### Tour Slides

```csharp
public List<TourSlide> TourSlides { get; } = new()
{
    new TourSlide
    {
        Image = "tour_transactions.png",
        Title = "Gestisci le Transazioni",
        Description = "Aggiungi, modifica ed elimina transazioni con un tap. Swipe per azioni rapide."
    },
    new TourSlide
    {
        Image = "tour_import.png",
        Title = "Importa Estratti Conto",
        Description = "Carica file CSV dalla tua banca e importa centinaia di transazioni in secondi."
    },
    new TourSlide
    {
        Image = "tour_analytics.png",
        Title = "Analizza con Grafici",
        Description = "Visualizza entrate, uscite e risparmio con grafici dettagliati mensili e annuali."
    },
    new TourSlide
    {
        Image = "tour_sync.png",
        Title = "Sincronizza con Desktop",
        Description = "Usa WiFi Sync per sincronizzare transazioni con l'app desktop. Dati sempre in locale!"
    }
};
```

**NextButtonText**: "Avanti" (slides 1-3), "Inizia!" (slide 4)

---

## ✅ Complete Onboarding

```csharp
private async Task CompleteOnboardingAsync()
{
    // Salva flag onboarding completato
    Preferences.Set("onboarding_completed", true);

    // Vai a MainPage (Dashboard)
    await Shell.Current.GoToAsync("//main");

    // Mostra success toast
    await Toast.Make("🎉 Benvenuto in MoneyMind!", ToastDuration.Short).Show();
}
```

---

## 🔄 Check First Launch (App.xaml.cs)

```csharp
protected override async void OnStart()
{
    base.OnStart();

    var onboardingCompleted = Preferences.Get("onboarding_completed", false);

    if (!onboardingCompleted)
    {
        // Prima apertura → Onboarding
        MainPage = new AppShell();
        await Shell.Current.GoToAsync("//onboarding/welcome");
    }
    else
    {
        // Utente esistente → Verifica licenza → MainPage
        var licenseValid = await _licenseService.ValidateLicenseAsync();

        if (licenseValid)
        {
            MainPage = new AppShell();
            await Shell.Current.GoToAsync("//main");
        }
        else
        {
            // Licenza scaduta/revocata → Forza riattivazione
            MainPage = new AppShell();
            await Shell.Current.GoToAsync("//onboarding/license");
        }
    }
}
```

---

## 🎨 UI Best Practices

### Animation Timings
- **Fade In**: 300ms
- **Slide Transition**: 250ms (CarouselView)
- **Button Press**: 150ms scale (1.0 → 0.95 → 1.0)

### Colors (Material 3)
- **Primary**: `#6750A4` (Purple)
- **Success**: `#4CAF50` (Green)
- **Background**: `#FFFBFE` (Light) / `#1C1B1F` (Dark)

### Typography
- **Headline**: 24sp Bold
- **Body**: 14sp Regular
- **Caption**: 12sp Regular

---

## 📊 Analytics Events (Optional)

Track user behavior per migliorare onboarding:

```csharp
// Track step completion
Analytics.TrackEvent("onboarding_step_completed", new Dictionary<string, string>
{
    { "step", "welcome" },
    { "timestamp", DateTime.Now.ToString() }
});

// Track skip actions
Analytics.TrackEvent("onboarding_skipped", new Dictionary<string, string>
{
    { "step", "biometric_setup" }
});

// Track completion time
Analytics.TrackEvent("onboarding_completed", new Dictionary<string, string>
{
    { "duration_seconds", _elapsedTime.TotalSeconds.ToString() }
});
```

---

## 🧪 Testing Onboarding

### Reset Onboarding (Debug)

```csharp
// SettingsPage → Debug Section
[RelayCommand]
private async Task ResetOnboardingAsync()
{
    var confirmed = await Shell.Current.DisplayAlert(
        "Reset Onboarding",
        "Questo cancellerà tutti i dati e riavvierà l'onboarding.",
        "Reset",
        "Annulla");

    if (confirmed)
    {
        Preferences.Clear();
        SecureStorage.RemoveAll();
        await _databaseService.DeleteAllDataAsync();

        // Restart app
        Application.Current.Quit();
    }
}
```

### Test Scenarios

1. **Happy Path**: User completa tutti gli step
2. **Skip Biometric**: User skips step 4
3. **Skip Tour**: User skips step 5
4. **License Error**: Invalid beta key → mostra error → retry
5. **Network Error**: No internet → mostra fallback → retry

---

## 📋 Implementation Checklist

- [ ] `WelcomePage.xaml` + ViewModel
- [ ] `LicenseActivationPage.xaml` + ViewModel
- [ ] `CreateAccountPage.xaml` + ViewModel
- [ ] `BiometricSetupPage.xaml` + ViewModel
- [ ] `QuickTourPage.xaml` + ViewModel
- [ ] AppShell routes per onboarding
- [ ] First launch check in App.xaml.cs
- [ ] Reset onboarding per testing
- [ ] Analytics tracking (optional)

---

**Tempo Implementazione Stimato**: 3-4 giorni

**Ultima Review**: 2025-01-XX
