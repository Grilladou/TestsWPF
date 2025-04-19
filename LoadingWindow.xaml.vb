Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Forms
Imports System.Windows.Interop
Imports System.Windows.Media
Imports System.Drawing ' Ajout explicite pour clarifier Rectangle
Imports System.Windows.Media.Animation ' Ajout pour Storyboard

Namespace HelloWorld
    ''' <summary>
    ''' Fenêtre de chargement pour informer l'utilisateur de la progression des opérations
    ''' et permettre l'annulation de celles-ci.
    ''' </summary>
    Public Class LoadingWindow
        Inherits Window

        ''' <summary>
        ''' Événement déclenché lorsque l'utilisateur demande l'arrêt de l'opération en cours.
        ''' </summary>
        Public Event StopRequested As EventHandler

        ' Constantes pour définir les proportions relatives
        Private Const WIDTH_RATIO As Double = 0.8 ' LoadingWindow sera 80% de la largeur de MainWindow
        Private Const MIN_WIDTH As Double = 300   ' Largeur minimale en pixels
        Private Const MIN_HEIGHT As Double = 280  ' Hauteur minimale en pixels pour permettre au bouton STOP d'être entièrement visible

        Private _owner As Window

        ''' <summary>
        ''' Constructeur par défaut.
        ''' </summary>
        Public Sub New()
            ' Cet appel est requis par le concepteur.
            InitializeComponent()

            ' S'abonner à l'événement de redimensionnement de cette fenêtre
            AddHandler Me.SizeChanged, AddressOf LoadingWindow_SizeChanged

            ' Remarque : Nous n'avons plus besoin de nous abonner à StopButton.StopClicked
            ' car nous utilisons maintenant directement l'événement Click du bouton standard
        End Sub
        ''' <summary>
        ''' Méthode pour permettre le déplacement de la fenêtre depuis la barre de titre.
        ''' </summary>
        Private Sub Border_MouseLeftButtonDown(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
            Me.DragMove()
        End Sub

        ''' <summary>
        ''' Gestionnaire pour le bouton de fermeture dans la barre de titre.
        ''' </summary>
        Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
            MyBase.Close()
        End Sub

        ''' <summary>
        ''' Méthode à appeler lorsque vous affichez la fenêtre de chargement
        ''' pour définir ses dimensions par rapport à son propriétaire.
        ''' </summary>
        Public Sub SetRelativeDimensions(owner As Window)
            _owner = owner

            ' S'abonner aux événements de redimensionnement de la fenêtre principale
            AddHandler _owner.SizeChanged, AddressOf Owner_SizeChanged

            ' Définir les dimensions initiales
            UpdateDimensions()
        End Sub

        ''' <summary>
        ''' Gestionnaire d'événement pour le redimensionnement de la fenêtre propriétaire.
        ''' </summary>
        Private Sub Owner_SizeChanged(sender As Object, e As SizeChangedEventArgs)
            ' Mettre à jour les dimensions quand MainWindow change de taille
            UpdateDimensions()
        End Sub

        ''' <summary>
        ''' Gestionnaire d'événement pour le redimensionnement de cette fenêtre.
        ''' </summary>
        Private Sub LoadingWindow_SizeChanged(sender As Object, e As SizeChangedEventArgs)
            ' Vérifier que la fenêtre reste dans les limites de l'écran
            EnsureWindowVisibility()
        End Sub

        ''' <summary>
        ''' Met à jour les dimensions de la fenêtre en fonction de son propriétaire
        ''' tout en respectant les dimensions minimales.
        ''' </summary>
        Private Sub UpdateDimensions()
            If _owner Is Nothing Then Return

            ' Calculer les nouvelles dimensions - conserver la largeur originale définie dans XAML si non minimisée
            Dim newWidth As Double
            If Not Double.IsNaN(Me.Width) AndAlso Me.Width >= MIN_WIDTH Then
                ' Garder la largeur actuelle mais s'assurer qu'elle n'est pas inférieure au minimum
                newWidth = Math.Max(Me.Width, MIN_WIDTH)
            Else
                ' Sinon calculer par rapport au parent
                newWidth = Math.Max(_owner.ActualWidth * WIDTH_RATIO, MIN_WIDTH)
            End If

            ' Même logique pour la hauteur - préserver la hauteur définie dans XAML si supérieure au minimum
            Dim newHeight As Double
            If Not Double.IsNaN(Me.Height) AndAlso Me.Height >= MIN_HEIGHT Then
                ' Garder la hauteur actuelle mais s'assurer qu'elle n'est pas inférieure au minimum
                newHeight = Math.Max(Me.Height, MIN_HEIGHT)
            Else
                ' Sinon calculer par rapport au parent mais toujours respecter le minimum
                newHeight = Math.Max(_owner.ActualHeight * 0.5, MIN_HEIGHT)
            End If

            ' Appliquer les nouvelles dimensions
            Me.Width = newWidth
            Me.Height = newHeight

            ' Recentrer la fenêtre par rapport au propriétaire
            If Me.IsLoaded Then
                CenterWindowToOwner()
            End If
        End Sub

        ''' <summary>
        ''' Centre la fenêtre par rapport à son propriétaire.
        ''' </summary>
        Private Sub CenterWindowToOwner()
            If _owner Is Nothing Then Return

            ' Calculer la position pour centrer la fenêtre par rapport au propriétaire
            Dim left As Double = _owner.Left + (_owner.Width - Me.Width) / 2
            Dim top As Double = _owner.Top + (_owner.Height - Me.Height) / 2

            Me.Left = left
            Me.Top = top
        End Sub

        ''' <summary>
        ''' Assure que la fenêtre reste visible à l'écran.
        ''' </summary>
        Private Sub EnsureWindowVisibility()
            ' S'assurer que la fenêtre reste visible sur l'écran
            Dim helper As New WindowInteropHelper(Me)
            Dim handle As IntPtr = helper.Handle
            Dim screen As Screen = Screen.FromHandle(handle)
            Dim workingArea As System.Drawing.Rectangle = screen.WorkingArea

            ' Convertir les coordonnées de l'écran en coordonnées WPF
            Dim source As PresentationSource = PresentationSource.FromVisual(Me)
            If source Is Nothing Then Return

            Dim transformToDevice As Matrix = source.CompositionTarget.TransformToDevice
            Dim dpiX As Double = transformToDevice.M11
            Dim dpiY As Double = transformToDevice.M22

            ' Utiliser les propriétés de System.Drawing.Rectangle (Right, Bottom, Left, Top)
            Dim screenRight As Double = workingArea.Right / dpiX
            Dim screenBottom As Double = workingArea.Bottom / dpiY
            Dim screenLeft As Double = workingArea.Left / dpiX
            Dim screenTop As Double = workingArea.Top / dpiY

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

        ''' <summary>
        ''' Méthode à appeler lors de la fermeture de la fenêtre pour nettoyer les ressources.
        ''' </summary>
        Private Sub Cleanup()
            If _owner IsNot Nothing Then
                RemoveHandler _owner.SizeChanged, AddressOf Owner_SizeChanged
                _owner = Nothing
            End If
        End Sub

        ''' <summary>
        ''' Surcharge de l'événement OnClosed pour nettoyer les ressources.
        ''' </summary>
        Protected Overrides Sub OnClosed(e As EventArgs)
            Cleanup()
            MyBase.OnClosed(e)
        End Sub

        ''' <summary>
        ''' Méthode appelée lorsque l'utilisateur clique sur le bouton STOP.
        ''' </summary>
        Private Sub StopButton_Click(sender As Object, e As RoutedEventArgs)
            ' Déclencher l'événement StopRequested pour informer MainWindow
            RaiseEvent StopRequested(Me, EventArgs.Empty)

            ' Mettre à jour l'état visuel pour indiquer que l'annulation est en cours
            SetStoppingState()
        End Sub

        ''' <summary>
        ''' Met à jour le message de chargement de manière thread-safe.
        ''' </summary>
        Public Sub UpdateLoadingMessage(message As String)
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(Sub() UpdateLoadingMessage(message))
                Return
            End If

            LoadingMessage.Text = message
        End Sub

        ''' <summary>
        ''' Met à jour le compteur de progression de manière thread-safe.
        ''' </summary>
        Public Sub UpdateProgressCounter(current As Integer, total As Integer)
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(Sub() UpdateProgressCounter(current, total))
                Return
            End If

            ProgressCounter.Text = $"{current}/{total}"
        End Sub

        ''' <summary>
        ''' Active l'état visuel d'arrêt (bouton désactivé, changement de texte, animation).
        ''' </summary>
        Public Sub SetStoppingState()
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(AddressOf SetStoppingState)
                Return
            End If

            ' Désactiver le bouton pour empêcher d'autres clics
            StopButton.IsEnabled = False
            StopButton.Content = "ARRÊT..."

            ' Créer et démarrer une animation de clignotement pour le bouton
            Dim storyboard As New Storyboard()
            Dim animation As New DoubleAnimation(1.0, 0.4, New Duration(TimeSpan.FromSeconds(0.5)))
            Storyboard.SetTarget(animation, StopButton)
            Storyboard.SetTargetProperty(animation, New PropertyPath("Opacity"))
            animation.AutoReverse = True
            animation.RepeatBehavior = RepeatBehavior.Forever
            storyboard.Children.Add(animation)
            storyboard.Begin()

            ' Changer la couleur de la barre de progression pour indiquer l'arrêt
            LoadingProgressBar.Foreground = New SolidColorBrush(Colors.Red)
        End Sub

        ''' <summary>
        ''' Réinitialise l'état visuel du bouton Stop.
        ''' </summary>
        Public Sub ResetStopButton()
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(AddressOf ResetStopButton)
                Return
            End If

            ' Réinitialiser l'état du bouton STOP
            StopButton.IsEnabled = True
            StopButton.Content = "STOP"
            StopButton.Opacity = 1.0 ' Arrêter l'animation en rétablissant l'opacité

            ' Réinitialiser la couleur de la barre de progression
            LoadingProgressBar.Foreground = New SolidColorBrush(System.Windows.Media.ColorConverter.ConvertFromString("#2979FF"))
        End Sub

        ''' <summary>
        ''' Ferme la fenêtre après un délai spécifié.
        ''' </summary>
        Public Sub CloseAfterDelay(delayMs As Integer)
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(Sub() CloseAfterDelay(delayMs))
                Return
            End If

            ' Définir un timer pour fermer la fenêtre après le délai
            Dim timer As New Threading.DispatcherTimer()
            timer.Interval = TimeSpan.FromMilliseconds(delayMs)

            AddHandler timer.Tick, Sub(s, args)
                                       timer.Stop()
                                       MyBase.Close()
                                   End Sub

            timer.Start()
        End Sub

        ''' <summary>
        ''' Méthode statique pour créer et afficher une fenêtre de chargement
        ''' liée à une fenêtre propriétaire.
        ''' </summary>
        Public Shared Function ShowRelativeTo(owner As Window, Optional message As String = Nothing) As LoadingWindow
            Dim window As New LoadingWindow()
            window.Owner = owner

            If Not String.IsNullOrEmpty(message) Then
                window.LoadingMessage.Text = message
            End If

            window.SetRelativeDimensions(owner)
            window.Show()

            Return window
        End Function
    End Class
End Namespace
