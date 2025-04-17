Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Input
Imports System.Windows.Interop
' Si vous avez besoin d'un import pour SettingsEventArgs, utilisez plutôt :
' Imports HelloWorldLeChat


Namespace HelloWorldLeChat

    Public Class MainWindow
        Inherits Window

        ' Storyboards pour les animations du panneau
        Private _openSettingsPanelStoryboard As Storyboard
        Private _closeSettingsPanelStoryboard As Storyboard

        ' État du panneau (ouvert/fermé)
        Private _isSettingsPanelOpen As Boolean = False

        ' Fantôme pour l'aperçu des dimensions
        Private _sizePreviewRectangle As Rectangle = Nothing


        Public Sub New()
            ' Cet appel est requis par le concepteur.
            InitializeComponent()

            ' Initialiser la zone de texte
            reportTextBox.Clear()

            ' Configurer les paramètres BaseInfoCollector avant la collecte
            ' Ces paramètres s'appliquent à toutes les classes qui héritent de BaseInfoCollector
            BaseInfoCollector.StepDelayInSeconds = 1  ' Attendre x secondes entre chaque étape
            BaseInfoCollector.ShowResultPopup = True ' Ne pas afficher de popup

            ' S'abonner à l'événement de progression
            AddHandler OSUtility.ProgressUpdate, AddressOf OnProgressUpdate

            ' Connecter l'événement Click du bouton STOP principal
            AddHandler MainStopButton.Click, AddressOf MainStopButton_Click

            ' Connecter l'événement Click du bouton des paramètres
            AddHandler SettingsButton.Click, AddressOf SettingsButton_Click

            ' Obtenir les références aux Storyboards
            _openSettingsPanelStoryboard = TryCast(FindResource("OpenSettingsPanel"), Storyboard)
            _closeSettingsPanelStoryboard = TryCast(FindResource("CloseSettingsPanel"), Storyboard)

            ' Initialiser le panneau de paramètres
            InitializeSettingsPanel()
        End Sub

        ' =========================================================
        ' MÉTHODES POUR LE PANNEAU DE PARAMÈTRES
        ' =========================================================

        ''' <summary>
        ''' Initialise le panneau de paramètres et configure ses événements
        ''' </summary>
        Private Sub InitializeSettingsPanel()
            ' Initialiser le panneau avec cette fenêtre
            SettingsPanel.Initialize(Me)

            ' S'abonner aux événements du panneau
            AddHandler SettingsPanel.PanelCloseRequested, AddressOf SettingsPanel_PanelCloseRequested
            AddHandler SettingsPanel.SettingsApplied, AddressOf SettingsPanel_SettingsApplied
            AddHandler SettingsPanel.SettingsReset, AddressOf SettingsPanel_SettingsReset
        End Sub

        ''' <summary>
        ''' Ouvre le panneau latéral avec animation
        ''' </summary>
        Private Sub OpenSettingsPanel()
            ' Afficher l'overlay semi-transparent
            SettingsOverlay.Visibility = Visibility.Visible

            ' Lancer l'animation d'ouverture
            _openSettingsPanelStoryboard.Begin()

            ' Mettre à jour l'état
            _isSettingsPanelOpen = True
        End Sub

        ''' <summary>
        ''' Ferme le panneau latéral avec animation
        ''' </summary>
        Private Sub CloseSettingsPanel()
            ' Lancer l'animation de fermeture
            _closeSettingsPanelStoryboard.Begin()

            ' Cacher l'overlay après l'animation
            AddHandler _closeSettingsPanelStoryboard.Completed, Sub(s, e)
                                                                    SettingsOverlay.Visibility = Visibility.Collapsed
                                                                    RemoveHandler _closeSettingsPanelStoryboard.Completed, s
                                                                End Sub
            ' Mettre à jour l'état
            _isSettingsPanelOpen = False

            ' Supprimer le rectangle d'aperçu s'il existe
            RemoveSizePreviewRectangle()
        End Sub

        ''' <summary>
        ''' Crée un rectangle pour prévisualiser la taille de la fenêtre
        ''' </summary>
        Private Sub CreateSizePreviewRectangle(width As Double, height As Double)
            ' Supprimer l'ancien rectangle s'il existe
            RemoveSizePreviewRectangle()

            ' Créer un nouveau rectangle
            _sizePreviewRectangle = New Rectangle With {
            .Stroke = New SolidColorBrush(Colors.DodgerBlue),
            .StrokeThickness = 2,
            .StrokeDashArray = New DoubleCollection From {4, 2},
            .Fill = New SolidColorBrush(Color.FromArgb(30, 30, 144, 255))
        }

            ' Position absolue sur l'écran
            Dim screenPos = Me.PointToScreen(New Point(0, 0))

            ' Calculer la position centrée
            Dim left As Double = screenPos.X + (Me.Width - width) / 2
            Dim top As Double = screenPos.Y + (Me.Height - height) / 2

            ' Définir les dimensions et la position
            Canvas.SetLeft(_sizePreviewRectangle, left)
            Canvas.SetTop(_sizePreviewRectangle, top)
            _sizePreviewRectangle.Width = width
            _sizePreviewRectangle.Height = height

            ' Ajouter le rectangle à un canvas de niveau supérieur (à créer ou obtenir)
            ' Note: Cette partie dépend de la structure de votre application
            ' Une approche pourrait être d'utiliser un AdornerLayer
        End Sub

        ''' <summary>
        ''' Supprime le rectangle de prévisualisation
        ''' </summary>
        Private Sub RemoveSizePreviewRectangle()
            If _sizePreviewRectangle IsNot Nothing Then
                ' Retirer du parent
                Dim parent = VisualTreeHelper.GetParent(_sizePreviewRectangle)
                If TypeOf parent Is Panel Then
                    DirectCast(parent, Panel).Children.Remove(_sizePreviewRectangle)
                End If

                _sizePreviewRectangle = Nothing
            End If
        End Sub

        ''' <summary>
        ''' Met à jour les dimensions de la fenêtre
        ''' </summary>
        Private Sub UpdateWindowDimensions(width As Double, height As Double)
            ' Appliquer les nouvelles dimensions
            Me.Width = width
            Me.Height = height

            ' Vérifier la position pour s'assurer que la fenêtre reste visible
            EnsureWindowVisibility()
        End Sub

        ''' <summary>
        ''' S'assure que la fenêtre reste visible sur l'écran
        ''' </summary>
        Private Sub EnsureWindowVisibility()
            ' Obtenir l'écran actuel
            Dim handle = New System.Windows.Interop.WindowInteropHelper(Me).Handle
            Dim screen = System.Windows.Forms.Screen.FromHandle(handle)
            Dim workingArea = screen.WorkingArea

            ' Convertir en coordonnées WPF
            Dim source = PresentationSource.FromVisual(Me)
            If source Is Nothing Then Return

            Dim transformToDevice = source.CompositionTarget.TransformToDevice
            Dim dpiX = transformToDevice.M11
            Dim dpiY = transformToDevice.M22

            Dim screenRight = workingArea.Right / dpiX
            Dim screenBottom = workingArea.Bottom / dpiY
            Dim screenLeft = workingArea.Left / dpiX
            Dim screenTop = workingArea.Top / dpiY

            ' Ajuster la position si nécessaire
            If Me.Left + Me.Width > screenRight Then
                Me.Left = screenRight - Me.Width
            End If

            If Me.Top + Me.Height > screenBottom Then
                Me.Top = screenBottom - Me.Height
            End If

            If Me.Left < screenLeft Then
                Me.Left = screenLeft
            End If

            If Me.Top < screenTop Then
                Me.Top = screenTop
            End If
        End Sub

        ' =========================================================
        ' GESTIONNAIRES D'ÉVÉNEMENTS
        ' =========================================================

        ''' <summary>
        ''' Gestionnaire pour le clic sur le bouton des paramètres
        ''' </summary>
        Private Sub SettingsButton_Click(sender As Object, e As RoutedEventArgs)
            If _isSettingsPanelOpen Then
                CloseSettingsPanel()
            Else
                OpenSettingsPanel()
            End If
        End Sub

        ''' <summary>
        ''' Gestionnaire pour le clic sur l'overlay (ferme le panneau)
        ''' </summary>
        Private Sub SettingsOverlay_MouseDown(sender As Object, e As MouseButtonEventArgs)
            If _isSettingsPanelOpen Then
                CloseSettingsPanel()
            End If
        End Sub

        ''' <summary>
        ''' Gestionnaire pour l'événement de fermeture du panneau
        ''' </summary>
        Private Sub SettingsPanel_PanelCloseRequested(sender As Object, e As EventArgs)
            CloseSettingsPanel()
        End Sub

        ''' <summary>
        ''' Gestionnaire pour l'événement d'application des paramètres
        ''' </summary>
        Private Sub SettingsPanel_SettingsApplied(sender As Object, e As SettingsEventArgs)
            ' Mettre à jour les dimensions de la fenêtre
            UpdateWindowDimensions(e.WindowWidth, e.WindowHeight)

            ' Fermer le panneau
            CloseSettingsPanel()
        End Sub

        ''' <summary>
        ''' Gestionnaire pour l'événement de réinitialisation des paramètres
        ''' </summary>
        Private Sub SettingsPanel_SettingsReset(sender As Object, e As EventArgs)
            ' Aucune action supplémentaire nécessaire ici car le panneau met déjà à jour ses valeurs
        End Sub




        Private loadingWindow As LoadingWindow = Nothing
        Private isCollecting As Boolean = False ' Pour suivre l'état de la collecte

        Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
            Me.Close()
        End Sub

        Private Sub ShowScreenCount_Click(sender As Object, e As RoutedEventArgs)

            ' Rafraîchir les informations sur les écrans (optionnel, déjà fait automatiquement)
            ScreenUtility.RefreshMonitorsInfo()

            ' Afficher le nombre d'écrans dans une MessageBox
            MessageBox.Show($"Nombre d'écrans détectés : {ScreenUtility.ScreenCount}",
                        "Informations écrans",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information)

            ' Afficher le rapport détaillé dans la TextBox
            reportTextBox.Text = ScreenUtility.GetDetailedReport()
        End Sub
        ' Gestionnaire d'événement pour le bouton STOP principal
        Private Sub MainStopButton_Click(sender As Object, e As RoutedEventArgs)
            ' Demander l'annulation de la collecte
            BaseInfoCollector.RequestCancellation()

            ' Mettre à jour l'interface pour indiquer que l'annulation est en cours
            reportTextBox.Text += Environment.NewLine + "Annulation de la collecte en cours..."
        End Sub

        Private Async Sub ShowOSInfo_Click(sender As Object, e As RoutedEventArgs)
            ' Effacer le contenu du TextBox avant de commencer une nouvelle collecte
            reportTextBox.Clear()

            ' Indiquer que la collecte est en cours
            isCollecting = True

            ' Désactiver les boutons pendant le chargement (sauf MainStopButton)
            DisableButtons(True)

            ' Réinitialiser l'état d'annulation avant de commencer
            BaseInfoCollector.ResetCancellation()

            ' Gérer la visibilité du bouton STOP principal en fonction de ShowResultPopup
            MainStopButton.Visibility = If(BaseInfoCollector.ShowResultPopup, Visibility.Collapsed, Visibility.Visible)

            ' Créer et afficher la fenêtre de chargement seulement si ShowResultPopup est True
            If BaseInfoCollector.ShowResultPopup Then
                loadingWindow = New LoadingWindow()
                loadingWindow.Owner = Me
                ' S'abonner à l'événement StopRequested
                AddHandler loadingWindow.StopRequested, AddressOf OnStopRequested
                loadingWindow.Show()
            End If

            ' Exécuter la collecte d'informations de manière asynchrone
            Dim report As String = Await OSUtility.GetReportAsync()

            ' Fermer la fenêtre de chargement si elle existe
            If loadingWindow IsNot Nothing Then
                ' Se désabonner de l'événement
                RemoveHandler loadingWindow.StopRequested, AddressOf OnStopRequested
                loadingWindow.Close()
                loadingWindow = Nothing
            End If

            ' Afficher le rapport détaillé dans la TextBox
            reportTextBox.Text = report

            ' Cacher le bouton STOP principal
            MainStopButton.Visibility = Visibility.Collapsed

            ' Indiquer que la collecte est terminée
            isCollecting = False

            ' Réactiver les boutons
            DisableButtons(False)

            ' Afficher un résumé dans une MessageBox seulement si ShowResultPopup est True
            If BaseInfoCollector.ShowResultPopup AndAlso Not BaseInfoCollector.CancellationRequested Then
                MessageBox.Show($"Système : {Environment.OSVersion.VersionString}{Environment.NewLine}" &
                   $"Architecture : {If(Environment.Is64BitOperatingSystem, "64-bit", "32-bit")}{Environment.NewLine}" &
                   $"Processeurs : {Environment.ProcessorCount}",
                   "Informations système",
                   MessageBoxButton.OK,
                   MessageBoxImage.Information)
            End If
        End Sub
        Private Async Sub ShowHardwareInfo_Click(sender As Object, e As RoutedEventArgs)
            ' Effacer le contenu du TextBox avant de commencer une nouvelle collecte
            reportTextBox.Clear()

            ' Indiquer que la collecte est en cours
            isCollecting = True

            ' Désactiver les boutons pendant le chargement (sauf MainStopButton)
            DisableButtons(True)

            ' Réinitialiser l'état d'annulation avant de commencer
            BaseInfoCollector.ResetCancellation()

            ' Gérer la visibilité du bouton STOP principal
            MainStopButton.Visibility = If(BaseInfoCollector.ShowResultPopup, Visibility.Collapsed, Visibility.Visible)

            ' Créer et afficher la fenêtre de chargement si nécessaire
            If BaseInfoCollector.ShowResultPopup Then
                loadingWindow = New LoadingWindow()
                loadingWindow.Owner = Me
                AddHandler loadingWindow.StopRequested, AddressOf OnStopRequested
                loadingWindow.Show()
            End If

            ' Exécuter la collecte d'informations de manière asynchrone
            Dim report As String = Await HardwareUtility.GetReportAsync()

            ' Fermer la fenêtre de chargement si elle existe
            If loadingWindow IsNot Nothing Then
                RemoveHandler loadingWindow.StopRequested, AddressOf OnStopRequested
                loadingWindow.Close()
                loadingWindow = Nothing
            End If

            ' Afficher le rapport détaillé dans la TextBox
            reportTextBox.Text = report

            ' Cacher le bouton STOP principal
            MainStopButton.Visibility = Visibility.Collapsed

            ' Indiquer que la collecte est terminée
            isCollecting = False

            ' Réactiver les boutons
            DisableButtons(False)
        End Sub
        Private Async Sub ShowUserInfo_Click(sender As Object, e As RoutedEventArgs)
            ' Effacer le contenu du TextBox avant de commencer une nouvelle collecte
            reportTextBox.Clear()

            ' Indiquer que la collecte est en cours
            isCollecting = True

            ' Désactiver les boutons pendant le chargement (sauf MainStopButton)
            DisableButtons(True)

            ' Réinitialiser l'état d'annulation avant de commencer
            BaseInfoCollector.ResetCancellation()

            ' Gérer la visibilité du bouton STOP principal
            MainStopButton.Visibility = If(BaseInfoCollector.ShowResultPopup, Visibility.Collapsed, Visibility.Visible)

            ' Créer et afficher la fenêtre de chargement si nécessaire
            If BaseInfoCollector.ShowResultPopup Then
                loadingWindow = New LoadingWindow()
                loadingWindow.Owner = Me
                AddHandler loadingWindow.StopRequested, AddressOf OnStopRequested
                loadingWindow.Show()
            End If

            ' Exécuter la collecte d'informations de manière asynchrone
            Dim report As String = Await UserUtility.GetReportAsync()

            ' Fermer la fenêtre de chargement si elle existe
            If loadingWindow IsNot Nothing Then
                RemoveHandler loadingWindow.StopRequested, AddressOf OnStopRequested
                loadingWindow.Close()
                loadingWindow = Nothing
            End If

            ' Afficher le rapport détaillé dans la TextBox
            reportTextBox.Text = report

            ' Cacher le bouton STOP principal
            MainStopButton.Visibility = Visibility.Collapsed

            ' Indiquer que la collecte est terminée
            isCollecting = False

            ' Réactiver les boutons
            DisableButtons(False)
        End Sub

        ' Activer/désactiver les boutons
        Private Sub DisableButtons(disabled As Boolean)
            Dim allButtons = New List(Of Button)()
            FindVisualChildren(Of Button)(Me, allButtons)

            For Each button As Button In allButtons
                ' Ne pas désactiver le bouton STOP principal pendant la collecte
                If button IsNot MainStopButton Then
                    button.IsEnabled = Not disabled
                End If
            Next
        End Sub

        ' Gestionnaire pour l'événement StopRequested
        Private Sub OnStopRequested(sender As Object, e As EventArgs)
            ' Afficher un message indiquant que la collecte a été interrompue
            reportTextBox.Dispatcher.Invoke(Sub()
                                                reportTextBox.Text = "Collecte en cours d'interruption..."
                                            End Sub)
        End Sub

        ' Gestionnaire pour l'événement de progression
        Private Sub OnProgressUpdate(message As String, current As Integer, total As Integer)
            If loadingWindow IsNot Nothing Then
                loadingWindow.UpdateLoadingMessage(message)
                loadingWindow.UpdateProgressCounter(current, total)
            End If
        End Sub

        ' Méthode utilitaire pour trouver les contrôles enfants d'un type spécifique
        Private Sub FindVisualChildren(Of T As DependencyObject)(parent As DependencyObject, results As List(Of T))
            If parent Is Nothing Then Return

            For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(parent) - 1
                Dim child As DependencyObject = VisualTreeHelper.GetChild(parent, i)
                If TypeOf child Is T Then
                    results.Add(DirectCast(child, T))
                End If
                FindVisualChildren(child, results)
            Next
        End Sub
    End Class
End Namespace