# UI/UX Guidelines - MoneyMindApp

**Design Philosophy**: *Minimal, Clear, Powerful*

## 🎨 Design Principles

### 1. Information Density
**Poche schermate, tante informazioni**:
- Ogni schermata mostra 3-5 metriche chiave
- Cards con header + valore + trend
- Collapsible sections per info secondarie
- Bottom sheets per azioni contestuali

### 2. Clarity First
**Tutto deve essere immediatamente comprensibile**:
- Icone universali (Material Design)
- Labels chiari in italiano
- Tooltips su tap lungo
- Empty states con suggerimenti azione

### 3. Fluid Interactions
**Zero friction**:
- Animazioni 200-300ms (smooth)
- Swipe gestures per azioni comuni
- Pull-to-refresh standard
- Haptic feedback su Android

### 4. Contextual Actions
**Azioni nel contesto, non menu nascosti**:
- FAB per azione primaria
- SwipeView per azioni su item
- Long press per menu contestuale
- Bottom app bar per azioni multiple

---

## 🎨 Visual Design System

### Color Palette (Material 3)

#### Light Theme
```xml
<!-- Primary Colors -->
<Color x:Key="Primary">#6750A4</Color>           <!-- Purple 500 -->
<Color x:Key="OnPrimary">#FFFFFF</Color>
<Color x:Key="PrimaryContainer">#EADDFF</Color>
<Color x:Key="OnPrimaryContainer">#21005E</Color>

<!-- Secondary Colors -->
<Color x:Key="Secondary">#625B71</Color>
<Color x:Key="OnSecondary">#FFFFFF</Color>
<Color x:Key="SecondaryContainer">#E8DEF8</Color>

<!-- Semantic Colors -->
<Color x:Key="Success">#2E7D32</Color>           <!-- Green 700 - Entrate -->
<Color x:Key="SuccessLight">#81C784</Color>      <!-- Green 300 -->
<Color x:Key="Error">#BA1A1A</Color>             <!-- Red 700 - Uscite -->
<Color x:Key="ErrorLight">#EF5350</Color>        <!-- Red 400 -->
<Color x:Key="Warning">#F57C00</Color>           <!-- Orange 700 -->
<Color x:Key="Info">#1976D2</Color>              <!-- Blue 700 -->

<!-- Surface Colors -->
<Color x:Key="Surface">#FFFBFE</Color>
<Color x:Key="SurfaceVariant">#E7E0EC</Color>
<Color x:Key="Background">#FFFBFE</Color>
<Color x:Key="OnBackground">#1C1B1F</Color>

<!-- Outline -->
<Color x:Key="Outline">#79747E</Color>
<Color x:Key="OutlineVariant">#CAC4D0</Color>
```

#### Dark Theme
```xml
<Color x:Key="PrimaryDark">#D0BCFF</Color>
<Color x:Key="SurfaceDark">#1C1B1F</Color>
<Color x:Key="BackgroundDark">#1C1B1F</Color>
<Color x:Key="OnBackgroundDark">#E6E1E5</Color>
<Color x:Key="SuccessDark">#81C784</Color>
<Color x:Key="ErrorDark">#EF5350</Color>
```

### Typography (Inter Font Family)

```xml
<!-- Display -->
<Style x:Key="DisplayLarge" TargetType="Label">
    <Setter Property="FontFamily" Value="InterBold" />
    <Setter Property="FontSize" Value="57" />
    <Setter Property="LineHeight" Value="64" />
</Style>

<!-- Headline -->
<Style x:Key="HeadlineLarge" TargetType="Label">
    <Setter Property="FontFamily" Value="InterBold" />
    <Setter Property="FontSize" Value="32" />
    <Setter Property="LineHeight" Value="40" />
</Style>

<Style x:Key="HeadlineMedium" TargetType="Label">
    <Setter Property="FontFamily" Value="InterSemiBold" />
    <Setter Property="FontSize" Value="28" />
    <Setter Property="LineHeight" Value="36" />
</Style>

<!-- Title -->
<Style x:Key="TitleLarge" TargetType="Label">
    <Setter Property="FontFamily" Value="InterSemiBold" />
    <Setter Property="FontSize" Value="22" />
    <Setter Property="LineHeight" Value="28" />
</Style>

<Style x:Key="TitleMedium" TargetType="Label">
    <Setter Property="FontFamily" Value="InterMedium" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="LineHeight" Value="24" />
    <Setter Property="LetterSpacing" Value="0.15" />
</Style>

<!-- Body -->
<Style x:Key="BodyLarge" TargetType="Label">
    <Setter Property="FontFamily" Value="InterRegular" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="LineHeight" Value="24" />
</Style>

<Style x:Key="BodyMedium" TargetType="Label">
    <Setter Property="FontFamily" Value="InterRegular" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LineHeight" Value="20" />
</Style>

<!-- Label (Buttons, Tabs) -->
<Style x:Key="LabelLarge" TargetType="Label">
    <Setter Property="FontFamily" Value="InterMedium" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LineHeight" Value="20" />
    <Setter Property="LetterSpacing" Value="0.1" />
</Style>
```

### Spacing System (8pt Grid)

```csharp
public static class Spacing
{
    public const int XXSmall = 4;   // 0.5x
    public const int XSmall = 8;    // 1x
    public const int Small = 12;    // 1.5x
    public const int Medium = 16;   // 2x (base)
    public const int Large = 24;    // 3x
    public const int XLarge = 32;   // 4x
    public const int XXLarge = 48;  // 6x
    public const int Huge = 64;     // 8x
}
```

### Elevation (Shadows)

```xml
<!-- Level 1: Cards -->
<Shadow Brush="Black" Opacity="0.08" Offset="0,1" Radius="3" />

<!-- Level 2: FAB, App Bar -->
<Shadow Brush="Black" Opacity="0.12" Offset="0,2" Radius="6" />

<!-- Level 3: Dialogs -->
<Shadow Brush="Black" Opacity="0.16" Offset="0,4" Radius="12" />
```

### Corner Radius

```csharp
public static class CornerRadius
{
    public const int Small = 8;    // Chips, Small buttons
    public const int Medium = 12;  // Cards, Input fields
    public const int Large = 16;   // Bottom sheets, Dialogs
    public const int XLarge = 28;  // FAB
}
```

---

## 📱 Component Library

### 1. Stats Card (Dashboard)

```xml
<Frame Style="{StaticResource CardStyle}" Padding="16">
    <VerticalStackLayout Spacing="8">
        <!-- Header -->
        <HorizontalStackLayout Spacing="8">
            <Image Source="icon_wallet.svg" WidthRequest="24" HeightRequest="24" />
            <Label Text="Saldo Totale" Style="{StaticResource LabelLarge}"
                   TextColor="{StaticResource OnSurfaceVariant}" />
        </HorizontalStackLayout>

        <!-- Value -->
        <Label Text="€2.450,00" Style="{StaticResource HeadlineMedium}"
               TextColor="{StaticResource OnSurface}" />

        <!-- Trend -->
        <HorizontalStackLayout Spacing="4">
            <Image Source="icon_trend_up.svg" WidthRequest="16" HeightRequest="16"
                   IsVisible="{Binding IsTrendPositive}" />
            <Label Text="+5% vs mese scorso" Style="{StaticResource BodyMedium}"
                   TextColor="{StaticResource Success}" />
        </HorizontalStackLayout>
    </VerticalStackLayout>
</Frame>
```

**Varianti**:
- Success card (verde) per Entrate
- Error card (rossa) per Uscite
- Info card (blu) per Risparmio
- Neutral card (grigia) per Saldo

### 2. Transaction List Item

```xml
<SwipeView>
    <!-- Left Swipe: Delete -->
    <SwipeView.LeftItems>
        <SwipeItems Mode="Execute">
            <SwipeItem Text="Elimina" BackgroundColor="{StaticResource Error}"
                       Command="{Binding DeleteCommand}" />
        </SwipeItems>
    </SwipeView.LeftItems>

    <!-- Right Swipe: Edit -->
    <SwipeView.RightItems>
        <SwipeItems Mode="Execute">
            <SwipeItem Text="Modifica" BackgroundColor="{StaticResource Primary}"
                       Command="{Binding EditCommand}" />
        </SwipeItems>
    </SwipeView.RightItems>

    <!-- Content -->
    <Grid Padding="16" ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
        <!-- Date Badge -->
        <Frame Grid.Column="0" CornerRadius="8" Padding="8"
               BackgroundColor="{StaticResource SurfaceVariant}">
            <VerticalStackLayout Spacing="0">
                <Label Text="15" Style="{StaticResource TitleMedium}"
                       HorizontalOptions="Center" />
                <Label Text="OTT" Style="{StaticResource BodyMedium}"
                       FontSize="10" HorizontalOptions="Center" />
            </VerticalStackLayout>
        </Frame>

        <!-- Description -->
        <VerticalStackLayout Grid.Column="1" Spacing="4" VerticalOptions="Center">
            <Label Text="{Binding Descrizione}" Style="{StaticResource BodyLarge}"
                   LineBreakMode="TailTruncation" MaxLines="1" />
            <Label Text="{Binding Causale}" Style="{StaticResource BodyMedium}"
                   TextColor="{StaticResource OnSurfaceVariant}"
                   IsVisible="{Binding HasCausale}" />
        </VerticalStackLayout>

        <!-- Amount -->
        <Label Grid.Column="2" Text="{Binding ImportoFormatted}"
               Style="{StaticResource TitleMedium}"
               TextColor="{Binding ImportoColor}"
               VerticalOptions="Center" />
    </Grid>
</SwipeView>
```

### 3. Bottom Sheet (Filters)

```xml
<Border Style="{StaticResource BottomSheetStyle}">
    <VerticalStackLayout Padding="24" Spacing="16">
        <!-- Handle -->
        <BoxView WidthRequest="32" HeightRequest="4" CornerRadius="2"
                 BackgroundColor="{StaticResource OnSurfaceVariant}"
                 HorizontalOptions="Center" Opacity="0.4" />

        <!-- Title -->
        <Label Text="Filtri Avanzati" Style="{StaticResource HeadlineMedium}" />

        <!-- Period -->
        <VerticalStackLayout Spacing="8">
            <Label Text="Periodo" Style="{StaticResource LabelLarge}" />
            <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                <DatePicker Grid.Column="0" Date="{Binding StartDate}" />
                <DatePicker Grid.Column="1" Date="{Binding EndDate}" />
            </Grid>
        </VerticalStackLayout>

        <!-- Amount Range -->
        <VerticalStackLayout Spacing="8">
            <Label Text="Importo" Style="{StaticResource LabelLarge}" />
            <RangeSlider Minimum="0" Maximum="5000"
                         MinimumValue="{Binding MinAmount}"
                         MaximumValue="{Binding MaxAmount}" />
            <HorizontalStackLayout Spacing="8" HorizontalOptions="Center">
                <Label Text="{Binding MinAmount, StringFormat='€{0:N2}'}" />
                <Label Text="-" />
                <Label Text="{Binding MaxAmount, StringFormat='€{0:N2}'}" />
            </HorizontalStackLayout>
        </VerticalStackLayout>

        <!-- Actions -->
        <Grid ColumnDefinitions="*,*" ColumnSpacing="12" Margin="0,16,0,0">
            <Button Grid.Column="0" Text="Reset" Style="{StaticResource OutlinedButtonStyle}"
                    Command="{Binding ResetCommand}" />
            <Button Grid.Column="1" Text="Applica" Style="{StaticResource FilledButtonStyle}"
                    Command="{Binding ApplyCommand}" />
        </Grid>
    </VerticalStackLayout>
</Border>
```

### 4. Empty State

```xml
<VerticalStackLayout Padding="32" Spacing="16"
                     HorizontalOptions="Center" VerticalOptions="Center">
    <!-- Illustration -->
    <Image Source="empty_transactions.svg" WidthRequest="200" HeightRequest="200" />

    <!-- Headline -->
    <Label Text="Nessuna transazione" Style="{StaticResource HeadlineMedium}"
           HorizontalOptions="Center" />

    <!-- Description -->
    <Label Text="Inizia aggiungendo la tua prima transazione o importa un file dalla tua banca"
           Style="{StaticResource BodyMedium}"
           TextColor="{StaticResource OnSurfaceVariant}"
           HorizontalTextAlignment="Center" />

    <!-- Action -->
    <Button Text="Aggiungi Transazione" Style="{StaticResource FilledButtonStyle}"
            Command="{Binding AddTransactionCommand}" />
</VerticalStackLayout>
```

### 5. Loading Skeleton

```xml
<!-- Skeleton per Transaction Item -->
<Grid Padding="16" ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
    <BoxView Grid.Column="0" WidthRequest="48" HeightRequest="48"
             CornerRadius="8" BackgroundColor="{StaticResource SurfaceVariant}">
        <BoxView.Behaviors>
            <toolkit:AnimationBehavior>
                <toolkit:FadeAnimation Duration="1000" Repeat="true" />
            </toolkit:AnimationBehavior>
        </BoxView.Behaviors>
    </BoxView>

    <VerticalStackLayout Grid.Column="1" Spacing="8" VerticalOptions="Center">
        <BoxView WidthRequest="180" HeightRequest="16" CornerRadius="4"
                 BackgroundColor="{StaticResource SurfaceVariant}" />
        <BoxView WidthRequest="120" HeightRequest="12" CornerRadius="4"
                 BackgroundColor="{StaticResource SurfaceVariant}" Opacity="0.6" />
    </VerticalStackLayout>

    <BoxView Grid.Column="2" WidthRequest="80" HeightRequest="20"
             CornerRadius="4" BackgroundColor="{StaticResource SurfaceVariant}" />
</Grid>
```

### 6. FAB (Floating Action Button)

```xml
<Button Style="{StaticResource FABStyle}"
        WidthRequest="56" HeightRequest="56"
        CornerRadius="28"
        BackgroundColor="{StaticResource Primary}"
        Command="{Binding AddTransactionCommand}"
        HorizontalOptions="End" VerticalOptions="End"
        Margin="0,0,16,16">
    <Button.Shadow>
        <Shadow Brush="Black" Opacity="0.2" Offset="0,4" Radius="8" />
    </Button.Shadow>
    <Image Source="icon_add.svg" WidthRequest="24" HeightRequest="24" />
</Button>
```

**Extended FAB** (con label):
```xml
<HorizontalStackLayout Spacing="12" Padding="16,0">
    <Image Source="icon_add.svg" WidthRequest="24" HeightRequest="24" />
    <Label Text="Nuova Transazione" Style="{StaticResource LabelLarge}"
           TextColor="{StaticResource OnPrimary}" />
</HorizontalStackLayout>
```

---

## 🎬 Animation Guidelines

### 1. Standard Durations

```csharp
public static class AnimationDuration
{
    public const int Quick = 100;      // Micro-interactions (ripple, hover)
    public const int Normal = 200;     // Page transitions, cards
    public const int Slow = 300;       // Modals, bottom sheets
    public const int VerySlow = 500;   // Complex animations, charts
}
```

### 2. Easing Curves

```csharp
// Material Motion System
Easing.CubicIn      // Accelerate (exit)
Easing.CubicOut     // Decelerate (enter)
Easing.CubicInOut   // Standard (shared element)
Easing.SinInOut     // Emphasized (important transitions)
```

### 3. Page Transitions

```csharp
// Slide Up (modal, bottom sheet)
await bottomSheet.TranslateTo(0, 0, 300, Easing.CubicOut);

// Fade In (content load)
await content.FadeTo(1, 200, Easing.CubicOut);

// Scale Up (card expand)
await card.ScaleTo(1, 200, Easing.CubicOut);
```

### 4. List Item Animations

```xml
<!-- Staggered Fade In -->
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <ContentView Opacity="0">
                <ContentView.Triggers>
                    <DataTrigger TargetType="ContentView" Binding="{Binding IsLoaded}" Value="True">
                        <DataTrigger.EnterActions>
                            <toolkit:FadeAnimation Opacity="1" Duration="200" Delay="{Binding Index, Converter={StaticResource IndexToDelayConverter}}" />
                        </DataTrigger.EnterActions>
                    </DataTrigger>
                </ContentView.Triggers>
                <!-- Item content -->
            </ContentView>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

### 5. Pull-to-Refresh Animation

```xml
<RefreshView IsRefreshing="{Binding IsRefreshing}"
             Command="{Binding RefreshCommand}"
             RefreshColor="{StaticResource Primary}">
    <CollectionView ItemsSource="{Binding Items}" />
</RefreshView>
```

---

## 📐 Layout Patterns

### 1. Dashboard Grid (Stats Cards)

```xml
<Grid RowDefinitions="Auto,Auto" ColumnDefinitions="*,*"
      RowSpacing="16" ColumnSpacing="16" Padding="16">
    <Frame Grid.Row="0" Grid.Column="0" Style="{StaticResource StatsCardStyle}">
        <!-- Saldo Totale -->
    </Frame>
    <Frame Grid.Row="0" Grid.Column="1" Style="{StaticResource StatsCardStyle}">
        <!-- Entrate -->
    </Frame>
    <Frame Grid.Row="1" Grid.Column="0" Style="{StaticResource StatsCardStyle}">
        <!-- Uscite -->
    </Frame>
    <Frame Grid.Row="1" Grid.Column="1" Style="{StaticResource StatsCardStyle}">
        <!-- Risparmio -->
    </Frame>
</Grid>
```

### 2. List with Sticky Header

```xml
<CollectionView ItemsSource="{Binding GroupedTransactions}" IsGrouped="True">
    <CollectionView.GroupHeaderTemplate>
        <DataTemplate>
            <Label Text="{Binding Key}" Style="{StaticResource TitleMedium}"
                   BackgroundColor="{StaticResource Surface}"
                   Padding="16,8" />
        </DataTemplate>
    </CollectionView.GroupHeaderTemplate>
    <CollectionView.ItemTemplate>
        <!-- Transaction item -->
    </CollectionView.ItemTemplate>
</CollectionView>
```

### 3. Master-Detail (Tablet/Desktop)

```xml
<Grid ColumnDefinitions="300,*">
    <!-- Master: Lista conti -->
    <CollectionView Grid.Column="0" ItemsSource="{Binding Accounts}"
                    SelectedItem="{Binding SelectedAccount}">
        <!-- Account items -->
    </CollectionView>

    <!-- Detail: Transazioni conto selezionato -->
    <ContentView Grid.Column="1" Content="{Binding SelectedAccountView}" />
</Grid>
```

---

## 🎯 UX Patterns

### 1. Progressive Disclosure

**Dashboard iniziale mostra solo stats essenziali**:
- Tap card → Expand dettagli (slide down)
- "Vedi tutte le transazioni" → Navigazione lista completa
- Chart minimale → Tap → Full-screen analytics

### 2. Contextual Help

```xml
<!-- Info icon con tooltip -->
<HorizontalStackLayout Spacing="4">
    <Label Text="Periodo Stipendiale" Style="{StaticResource LabelLarge}" />
    <Image Source="icon_info.svg" WidthRequest="16" HeightRequest="16">
        <Image.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding ShowTooltipCommand}"
                                  CommandParameter="Il periodo va dal giorno di pagamento al giorno prima del prossimo pagamento" />
        </Image.GestureRecognizers>
    </Image>
</HorizontalStackLayout>
```

### 3. Smart Defaults

- **Import**: Auto-detect ultima configurazione usata
- **Filtri**: Default "Mese corrente" (periodo stipendiale)
- **Export**: Default formato CSV (più comune)
- **Date picker**: Default data odierna

### 4. Undo Actions

```xml
<!-- Snackbar dopo eliminazione -->
<Frame Style="{StaticResource SnackbarStyle}" IsVisible="{Binding ShowUndoSnackbar}">
    <HorizontalStackLayout Spacing="16">
        <Label Text="Transazione eliminata" Style="{StaticResource BodyMedium}"
               TextColor="{StaticResource OnSurface}" />
        <Button Text="ANNULLA" Style="{StaticResource TextButtonStyle}"
                Command="{Binding UndoDeleteCommand}" />
    </HorizontalStackLayout>
</Frame>
```

**Timeout**: 5 secondi → commit definitivo

### 5. Offline Mode

```xml
<!-- Banner offline -->
<Frame IsVisible="{Binding IsOffline}" BackgroundColor="{StaticResource Warning}"
       Padding="12" CornerRadius="0">
    <HorizontalStackLayout Spacing="8">
        <Image Source="icon_wifi_off.svg" WidthRequest="20" HeightRequest="20" />
        <Label Text="Modalità offline. Le modifiche saranno sincronizzate quando torni online."
               Style="{StaticResource BodyMedium}" TextColor="{StaticResource OnPrimary}" />
    </HorizontalStackLayout>
</Frame>
```

**Funzionalità offline**:
- ✅ Visualizzazione dati (cache locale)
- ✅ Modifica transazioni (queue sync)
- ✅ Statistiche (calcolo locale)
- ❌ License check (grace 7 giorni)
- ❌ Export cloud (solo locale)

---

## 📱 Platform-Specific Guidelines

### Android

#### Material You (Dynamic Colors)
```csharp
// MainActivity.cs
protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Enable edge-to-edge
    Window.SetDecorFitsSystemWindows(false);

    // Dynamic colors (Android 12+)
    if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
    {
        Application.Current.Resources.MergedDictionaries.Add(
            new DynamicColorsResourceDictionary());
    }
}
```

#### Status Bar
```csharp
// Trasparente con scrim
<item name="android:statusBarColor">@android:color/transparent</item>
<item name="android:windowLightStatusBar">true</item> <!-- Light theme -->
```

#### Navigation Bar
```csharp
// Gesture navigation (Android 10+)
<item name="android:navigationBarColor">@android:color/transparent</item>
<item name="android:windowLayoutInDisplayCutoutMode">shortEdges</item>
```

### iOS

#### Safe Area
```xml
<!-- Respect notch/island -->
<ContentPage xmlns:ios="clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls"
             ios:Page.UseSafeArea="True">
```

#### Navigation Bar
```csharp
// Large titles (iOS standard)
Shell.SetNavBarIsVisible(this, true);
Shell.SetNavBarHasShadow(this, false);
```

#### Haptics
```csharp
// Feedback su azioni (iOS style)
HapticFeedback.Perform(HapticFeedbackType.Click); // Light tap
HapticFeedback.Perform(HapticFeedbackType.LongPress); // Medium tap
```

### Windows

#### Fluent Design
```xml
<!-- Acrylic background (blur) -->
<Frame Background="{ThemeResource SystemControlAcrylicWindowBrush}"
       Padding="20" CornerRadius="8" />
```

#### Title Bar
```csharp
// Custom title bar (Windows 11)
AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
```

---

## ♿ Accessibility

### 1. Semantic Properties

```xml
<Label Text="Saldo Totale"
       SemanticProperties.HeadingLevel="Level1"
       SemanticProperties.Description="Il saldo totale del conto corrente selezionato" />

<Button Text="Aggiungi"
        SemanticProperties.Hint="Tap per aggiungere una nuova transazione" />

<Image Source="logo.png"
       SemanticProperties.Description="Logo MoneyMind" />
```

### 2. Minimum Touch Target

**44x44 dp** (iOS), **48x48 dp** (Android)

```xml
<Button Text="+" WidthRequest="48" HeightRequest="48" />
```

### 3. Color Contrast

- **Text**: Minimum 4.5:1 (WCAG AA)
- **Large text**: Minimum 3:1
- **Icons**: Minimum 3:1

**Verifica**: https://webaim.org/resources/contrastchecker/

### 4. Focus Indicators

```xml
<VisualStateGroup x:Name="CommonStates">
    <VisualState x:Name="Normal" />
    <VisualState x:Name="Focused">
        <VisualState.Setters>
            <Setter Property="BorderColor" Value="{StaticResource Primary}" />
            <Setter Property="BorderWidth" Value="2" />
        </VisualState.Setters>
    </VisualState>
</VisualStateGroup>
```

---

## 🧪 Usability Testing Checklist

### Onboarding
- [ ] First launch chiede permessi (se necessari)
- [ ] Setup conto rapido (< 1 minuto)
- [ ] Tutorial interattivo (opzionale, skipable)

### Navigation
- [ ] Tutte le schermate raggiungibili in max 3 tap
- [ ] Back button ritorna sempre alla schermata precedente
- [ ] Bottom nav evidenzia tab attiva

### Feedback
- [ ] Ogni azione ha feedback visivo (spinner, checkmark)
- [ ] Errori mostrano messaggio chiaro + suggerimento fix
- [ ] Success states celebrano azione (animazione, colore)

### Performance
- [ ] Cold start < 2s
- [ ] Lista 1000 item scroll fluido (60fps)
- [ ] Nessun blocco UI durante operazioni async

### Edge Cases
- [ ] Empty states con CTA chiare
- [ ] Offline mode degrada gracefully
- [ ] Error states permettono retry

---

## 📚 Risorse Design

### Iconography
- [Material Symbols](https://fonts.google.com/icons) - 2500+ icons
- [Phosphor Icons](https://phosphoricons.com/) - Moderne, outline/filled

### Illustrations
- [Undraw](https://undraw.co/) - Customizable illustrations
- [Storyset](https://storyset.com/) - Animated illustrations

### Mockup Tools
- [Figma](https://figma.com) - Design collaborativo
- [Mobbin](https://mobbin.com) - Ispirazione app finance

### Testing
- [Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Material Theme Builder](https://m3.material.io/theme-builder)

---

**Principio Guida**: *"Se l'utente deve pensare più di 2 secondi per capire cosa fare, abbiamo fallito."*
