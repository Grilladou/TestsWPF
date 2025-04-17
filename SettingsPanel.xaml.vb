Imports System.ComponentModel
Imports System.Windows
Imports System.Windows.Controls ' Pour les contrôles WPF
Imports System.Windows.Media.Animation
Imports System.Text.Json
Imports System.IO
Imports System.Collections.Generic
Imports System.Diagnostics
Imports MessageBox = System.Windows.MessageBox
' Nous avons besoin de Screen de Windows.Forms, mais utilisons un alias pour éviter les conflits
Imports WinForms = System.Windows.Forms

Namespace HelloWorldLeChat
    ''' <summary>
    ''' Panneau latéral de paramètres avec des fonctionnalités de configuration pour l'application principale
    ''' </summary>
    Public Class SettingsPanel
        Implements INotifyPropertyChanged

        ' =========================================================
        ' ÉVÉNEMENTS ET DÉLÉGUÉS
        ' =========================================================

        ''' <summary>
        ''' Événement requis pour l'implémentation de INotifyPropertyChanged
        ''' </summary>
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        ''' <summary>
        ''' Événement déclenché lorsque les paramètres doivent être appliqués
        ''' </summary>
        Public Event SettingsApplied As EventHandler(Of SettingsEventArgs)

        ''' <summary>
        ''' Événement déclenché lorsque les paramètres sont réinitialisés
        ''' </summary>
        Public Event SettingsReset As EventHandler

        ''' <summary>
        ''' Événement déclenché lorsque le panneau doit être fermé
        ''' </summary>
        Public Event PanelCloseRequested As EventHandler

        ' =========================================================
        ' CONSTANTES ET VALEURS PAR DÉFAUT
        ' =========================================================

        ''' <summary>
        ''' Chemin du fichier de configuration
        ''' </summary>
        Private Const CONFIG_FILE_NAME As String = "app_settings.json"

        ''' <summary>
        ''' Largeur par défaut de la fenêtre
        ''' </summary>
        Private Const DEFAULT_WINDOW_WIDTH As Double = 500

        ''' <summary>
        ''' Hauteur par défaut de la fenêtre
        ''' </summary>
        Private Const DEFAULT_WINDOW_HEIGHT As Double = 450

        ' =========================================================
        ' PROPRIÉTÉS PRIVÉES
        ' =========================================================

        ''' <summary>
        ''' Indique si la popup de chargement doit être affichée
        ''' </summary>
        Private _showLoadingPopup As Boolean = True

        ''' <summary>
        ''' Indique si les paramètres doivent être sauvegardés
        ''' </summary>
        Private _saveSettings As Boolean = False

        ''' <summary>
        ''' Indique si l'aperçu en temps réel est activé
        ''' </summary>
        Private _livePreview As Boolean = True

        ''' <summary>
        ''' Largeur souhaitée pour la fenêtre principale
        ''' </summary>
        Private _windowWidth As Double = DEFAULT_WINDOW_WIDTH

        ''' <summary>
        ''' Hauteur souhaitée pour la fenêtre principale
        ''' </summary>
        Private _windowHeight As Double = DEFAULT_WINDOW_HEIGHT

        ''' <summary>
        ''' Indique si les dimensions doivent être ajustées automatiquement
        ''' </summary>
        Private _autoAdjust As Boolean = True

        ''' <summary>
        ''' Informations sur l'écran actuel (dimensions, position)
        ''' </summary>
        Private _currentScreenInfo As String = "Chargement des informations..."

        ''' <summary>
        ''' Message d'avertissement concernant les dimensions d'écran
        ''' </summary>
        Private _screenWarning As String = String.Empty

        ''' <summary>
        ''' Indique s'il y a un avertissement à afficher concernant l'écran
        ''' </summary>
        Private _hasScreenWarning As Boolean = False

        ''' <summary>
        ''' Référence à la fenêtre principale
        ''' </summary>
        Private _mainWindow As Window

        ' =========================================================
        ' PROPRIÉTÉS PUBLIQUES LIABLES (BINDABLE PROPERTIES)
        ' =========================================================

        ''' <summary>
        ''' Obtient ou définit si la popup de chargement doit être affichée
        ''' </summary>
        Public Property ShowLoadingPopup As Boolean
            Get
                Return _showLoadingPopup
            End Get
            Set(value As Boolean)
                If _showLoadingPopup <> value Then
                    _showLoadingPopup = value
                    OnPropertyChanged(NameOf(ShowLoadingPopup))

                    ' Appliquer immédiatement ce paramètre
                    BaseInfoCollector.ShowResultPopup = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit si les paramètres doivent être sauvegardés
        ''' </summary>
        Public Property SaveSettings As Boolean
            Get
                Return _saveSettings
            End Get
            Set(value As Boolean)
                If _saveSettings <> value Then
                    _saveSettings = value
                    OnPropertyChanged(NameOf(SaveSettings))

                    ' Si activé, sauvegarder immédiatement les paramètres
                    If value Then
                        SaveSettingsToFile()
                    End If
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit si l'aperçu en temps réel est activé
        ''' </summary>
        Public Property LivePreview As Boolean
            Get
                Return _livePreview
            End Get
            Set(value As Boolean)
                If _livePreview <> value Then
                    _livePreview = value
                    OnPropertyChanged(NameOf(LivePreview))
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit la largeur de la fenêtre
        ''' </summary>
        Public Property WindowWidth As Double
            Get
                Return _windowWidth
            End Get
            Set(value As Double)
                If _windowWidth <> value Then
                    _windowWidth = value
                    OnPropertyChanged(NameOf(WindowWidth))
                    CheckScreenBoundaries()

                    ' Si l'aperçu en temps réel est activé et les dimensions sont valides
                    If LivePreview AndAlso Not HasScreenWarning Then
                        PreviewWindowSize()
                    End If
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit la hauteur de la fenêtre
        ''' </summary>
        Public Property WindowHeight As Double
            Get
                Return _windowHeight
            End Get
            Set(value As Double)
                If _windowHeight <> value Then
                    _windowHeight = value
                    OnPropertyChanged(NameOf(WindowHeight))
                    CheckScreenBoundaries()

                    ' Si l'aperçu en temps réel est activé et les dimensions sont valides
                    If LivePreview AndAlso Not HasScreenWarning Then
                        PreviewWindowSize()
                    End If
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit si les dimensions doivent être ajustées automatiquement
        ''' </summary>
        Public Property AutoAdjust As Boolean
            Get
                Return _autoAdjust
            End Get
            Set(value As Boolean)
                If _autoAdjust <> value Then
                    _autoAdjust = value
                    OnPropertyChanged(NameOf(AutoAdjust))

                    ' Si activé, ajuster immédiatement les dimensions
                    If value Then
                        AdjustDimensionsToScreen()
                    End If
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit les informations sur l'écran actuel
        ''' </summary>
        Public Property CurrentScreenInfo As String
            Get
                Return _currentScreenInfo
            End Get
            Set(value As String)
                If _currentScreenInfo <> value Then
                    _currentScreenInfo = value
                    OnPropertyChanged(NameOf(CurrentScreenInfo))
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit le message d'avertissement concernant l'écran
        ''' </summary>
        Public Property ScreenWarning As String
            Get
                Return _screenWarning
            End Get
            Set(value As String)
                If _screenWarning <> value Then
                    _screenWarning = value
                    OnPropertyChanged(NameOf(ScreenWarning))
                    HasScreenWarning = Not String.IsNullOrEmpty(value)
                End If
            End Set
        End Property

        ''' <summary>
        ''' Obtient ou définit s'il y a un avertissement à afficher
        ''' </summary>
        Public Property HasScreenWarning As Boolean
            Get
                Return _hasScreenWarning
            End Get
            Set(value As Boolean)
                If _hasScreenWarning <> value Then
                    _hasScreenWarning = value
                    OnPropertyChanged(NameOf(HasScreenWarning))
                End If
            End Set
        End Property

        ' =========================================================
        ' CONSTRUCTEURS ET INITIALISATION
        ' =========================================================

        ''' <summary>
        ''' Initialise une nouvelle instance de la classe SettingsPanel
        ''' </summary>
        Public Sub New()
            ' Cet appel est requis par le concepteur
            InitializeComponent()

            ' Définir le DataContext sur cette instance
            DataContext = Me

            ' Initialiser les valeurs par défaut
            LoadDefaultValues()
        End Sub

        ''' <summary>
        ''' Initialise le panneau pour une fenêtre principale spécifique
        ''' </summary>
        ''' <param name="window">La fenêtre principale</param>
        Public Sub Initialize(window As Window)
            _mainWindow = window

            ' Récupérer les dimensions actuelles
            WindowWidth = window.Width
            WindowHeight = window.Height

            ' Configurer les curseurs avec des limites appropriées
            UpdateSliderBounds()

            ' Charger les informations d'écran
            RefreshScreenInfo()

            ' Charger les paramètres sauvegardés s'ils existent
            LoadSettingsFromFile()

            ' Initialiser la valeur de ShowLoadingPopup à partir de BaseInfoCollector
            _showLoadingPopup = BaseInfoCollector.ShowResultPopup
            OnPropertyChanged(NameOf(ShowLoadingPopup))
        End Sub

        ' =========================================================
        ' MÉTHODES PRIVÉES
        ' =========================================================

        ''' <summary>
        ''' Charge les valeurs par défaut pour les paramètres
        ''' </summary>
        Private Sub LoadDefaultValues()
            WindowWidth = DEFAULT_WINDOW_WIDTH
            WindowHeight = DEFAULT_WINDOW_HEIGHT
            ShowLoadingPopup = True
            SaveSettings = False
            LivePreview = True
            AutoAdjust = True
        End Sub

        ''' <summary>
        ''' Met à jour les bornes des curseurs en fonction de l'écran actuel
        ''' </summary>
        Private Sub UpdateSliderBounds()
            If _mainWindow Is Nothing Then Return

            ' Obtenir l'écran actuel
            Dim screen As WinForms.Screen = WinForms.Screen.FromHandle(New System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle)
            Dim workingArea = screen.WorkingArea

            ' Définir les valeurs maximales
            WidthSlider.Maximum = workingArea.Width * 0.9
            HeightSlider.Maximum = workingArea.Height * 0.9

            ' Valeurs minimales raisonnables
            WidthSlider.Minimum = 300
            HeightSlider.Minimum = 200
        End Sub

        ''' <summary>
        ''' Rafraîchit les informations sur l'écran actuel
        ''' </summary>
        Private Sub RefreshScreenInfo()
            If _mainWindow Is Nothing Then Return

            ' Obtenir l'écran actuel
            Dim screen As WinForms.Screen = WinForms.Screen.FromHandle(New System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle)
            Dim workingArea = screen.WorkingArea

            ' Mettre à jour les informations d'écran
            CurrentScreenInfo = $"Écran actuel: {screen.DeviceName}" & vbCrLf &
                      $"Dimensions: {workingArea.Width} x {workingArea.Height}" & vbCrLf &
                      $"Position: ({workingArea.X}, {workingArea.Y})"

            ' Vérifier les limites
            CheckScreenBoundaries()
        End Sub

        ''' <summary>
        ''' Vérifie si les dimensions actuelles dépassent les limites de l'écran
        ''' </summary>
        Private Sub CheckScreenBoundaries()
            If _mainWindow Is Nothing Then Return

            ' Obtenir l'écran actuel
            Dim screen As WinForms.Screen = WinForms.Screen.FromHandle(New System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle)
            Dim workingArea = screen.WorkingArea

            ' Vérifier les dimensions
            Dim warnings As New List(Of String)

            If WindowWidth > workingArea.Width * 0.9 Then
                warnings.Add($"Largeur ({WindowWidth:F0}) dépasse 90% de l'écran ({workingArea.Width * 0.9:F0})")
            End If

            If WindowHeight > workingArea.Height * 0.9 Then
                warnings.Add($"Hauteur ({WindowHeight:F0}) dépasse 90% de l'écran ({workingArea.Height * 0.9:F0})")
            End If

            ' Mise à jour de l'avertissement
            If warnings.Count > 0 Then
                ScreenWarning = $"Attention: {String.Join(", ", warnings)}"

                ' Si l'ajustement automatique est activé, ajuster les dimensions
                If AutoAdjust Then
                    AdjustDimensionsToScreen()
                End If
            Else
                ScreenWarning = String.Empty
            End If
        End Sub

        ''' <summary>
        ''' Ajuste automatiquement les dimensions pour rester dans les limites de l'écran
        ''' </summary>
        Private Sub AdjustDimensionsToScreen()
            If _mainWindow Is Nothing Then Return

            ' Obtenir l'écran actuel
            Dim screen As WinForms.Screen = WinForms.Screen.FromHandle(New System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle)
            Dim workingArea = screen.WorkingArea

            ' Ajuster la largeur si nécessaire
            If WindowWidth > workingArea.Width * 0.9 Then
                WindowWidth = workingArea.Width * 0.9
            End If

            ' Ajuster la hauteur si nécessaire
            If WindowHeight > workingArea.Height * 0.9 Then
                WindowHeight = workingArea.Height * 0.9
            End If

            ' Effacer l'avertissement puisque les dimensions sont maintenant valides
            ScreenWarning = String.Empty
        End Sub

        ''' <summary>
        ''' Applique un aperçu des dimensions à la fenêtre principale si l'aperçu en temps réel est activé
        ''' </summary>
        Private Sub PreviewWindowSize()
            If _mainWindow Is Nothing OrElse Not LivePreview Then Return

            ' Appliquer temporairement les nouvelles dimensions
            _mainWindow.Width = WindowWidth
            _mainWindow.Height = WindowHeight
        End Sub

        ''' <summary>
        ''' Sauvegarde les paramètres dans un fichier JSON
        ''' </summary>
        Private Sub SaveSettingsToFile()
            Try
                ' Créer un objet anonyme avec les paramètres à sauvegarder
                Dim settings = New With {
                    .WindowWidth = WindowWidth,
                    .WindowHeight = WindowHeight,
                    .ShowLoadingPopup = ShowLoadingPopup,
                    .LivePreview = LivePreview,
                    .AutoAdjust = AutoAdjust
                }

                ' Sérialiser en JSON avec une indentation pour la lisibilité
                Dim options = New JsonSerializerOptions With {
                    .WriteIndented = True
                }
                Dim jsonString = JsonSerializer.Serialize(settings, options)

                ' Écrire dans le fichier
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME), jsonString)
            Catch ex As Exception
                ' En cas d'erreur, simplement tracer l'exception
                Debug.WriteLine($"Erreur lors de la sauvegarde des paramètres: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Charge les paramètres depuis un fichier JSON s'il existe
        ''' </summary>
        Private Sub LoadSettingsFromFile()
            Try
                Dim filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME)

                ' Vérifier si le fichier existe
                If File.Exists(filePath) Then
                    ' Lire le contenu du fichier
                    Dim jsonString = File.ReadAllText(filePath)

                    ' Désérialiser les paramètres
                    Dim options = New JsonSerializerOptions With {
                        .PropertyNameCaseInsensitive = True
                    }

                    ' Utiliser des variables temporaires pour éviter les notifications pendant le chargement
                    Dim tempSettings = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(jsonString, options)

                    ' Appliquer les paramètres chargés
                    If tempSettings.ContainsKey("WindowWidth") Then
                        _windowWidth = tempSettings("WindowWidth").GetDouble()
                    End If

                    If tempSettings.ContainsKey("WindowHeight") Then
                        _windowHeight = tempSettings("WindowHeight").GetDouble()
                    End If

                    If tempSettings.ContainsKey("ShowLoadingPopup") Then
                        _showLoadingPopup = tempSettings("ShowLoadingPopup").GetBoolean()
                    End If

                    If tempSettings.ContainsKey("LivePreview") Then
                        _livePreview = tempSettings("LivePreview").GetBoolean()
                    End If

                    If tempSettings.ContainsKey("AutoAdjust") Then
                        _autoAdjust = tempSettings("AutoAdjust").GetBoolean()
                    End If

                    ' Notifier les changements de toutes les propriétés
                    OnPropertyChanged(Nothing) ' Mettre à jour toutes les propriétés

                    ' Appliquer le paramètre à BaseInfoCollector
                    BaseInfoCollector.ShowResultPopup = _showLoadingPopup

                    ' Vérifier les limites d'écran
                    CheckScreenBoundaries()
                End If
            Catch ex As Exception
                ' En cas d'erreur, simplement tracer l'exception
                Debug.WriteLine($"Erreur lors du chargement des paramètres: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Méthode auxiliaire pour notifier qu'une propriété a changé
        ''' </summary>
        ''' <param name="propertyName">Nom de la propriété modifiée</param>
        Protected Sub OnPropertyChanged(propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

        ' =========================================================
        ' GESTIONNAIRES D'ÉVÉNEMENTS
        ' =========================================================

        ''' <summary>
        ''' Gestionnaire d'événement pour le clic sur le bouton de fermeture
        ''' </summary>
        Private Sub CloseSettingsButton_Click(sender As Object, e As RoutedEventArgs) Handles CloseSettingsButton.Click
            ' Notifier que le panneau doit être fermé
            RaiseEvent PanelCloseRequested(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Gestionnaire d'événement pour le clic sur le bouton Appliquer
        ''' </summary>
        Private Sub ApplyButton_Click(sender As Object, e As RoutedEventArgs) Handles ApplyButton.Click
            ' Vérifier si les dimensions sont valides
            If HasScreenWarning AndAlso Not AutoAdjust Then
                ' Demander confirmation à l'utilisateur
                Dim result = MessageBox.Show(
                    "Les dimensions que vous avez choisies dépassent les limites recommandées de l'écran. " &
                    "Voulez-vous les ajuster automatiquement ?",
                    "Dimensions hors limites",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning)

                If result = MessageBoxResult.Yes Then
                    ' Ajuster automatiquement
                    AdjustDimensionsToScreen()
                ElseIf result = MessageBoxResult.Cancel Then
                    ' Annuler l'opération
                    Return
                End If
                ' Si Non, continuer avec les dimensions actuelles
            End If

            ' Appliquer les paramètres
            BaseInfoCollector.ShowResultPopup = ShowLoadingPopup

            ' Sauvegarder les paramètres si demandé
            If SaveSettings Then
                SaveSettingsToFile()
            End If

            ' Déclencher l'événement SettingsApplied
            Dim args = New SettingsEventArgs With {
                .WindowWidth = WindowWidth,
                .WindowHeight = WindowHeight
            }

            RaiseEvent SettingsApplied(Me, args)
        End Sub

        ''' <summary>
        ''' Gestionnaire d'événement pour le clic sur le bouton Réinitialiser
        ''' </summary>
        Private Sub ResetButton_Click(sender As Object, e As RoutedEventArgs) Handles ResetButton.Click
            ' Réinitialiser les valeurs aux défauts
            LoadDefaultValues()

            ' Mettre à jour la propriété dans BaseInfoCollector
            BaseInfoCollector.ShowResultPopup = ShowLoadingPopup

            ' Déclencher l'événement SettingsReset
            RaiseEvent SettingsReset(Me, EventArgs.Empty)
        End Sub
    End Class
End Namespace