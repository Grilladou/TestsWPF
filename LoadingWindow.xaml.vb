' Dans le fichier code-behind de LoadingWindow.xaml.vb
Imports System
Imports System.Windows
Imports System.Windows.Forms
Imports System.Windows.Interop
Imports System.Windows.Media

Public Class LoadingWindow
    Public Event StopRequested As EventHandler

    ' Constantes pour définir les proportions relatives
    Private Const WIDTH_RATIO As Double = 0.8 ' LoadingWindow sera 80% de la largeur de MainWindow
    Private Const MIN_WIDTH As Double = 300   ' Largeur minimale en pixels
    Private Const MIN_HEIGHT As Double = 180  ' Hauteur minimale en pixels

    Private _owner As Window

    Public Sub New()
        ' Cet appel est requis par le concepteur
        InitializeComponent()

        ' S'abonner à l'événement de redimensionnement de cette fenêtre
        AddHandler Me.SizeChanged, AddressOf LoadingWindow_SizeChanged

        ' Connecter l'événement Click au gestionnaire
        AddHandler StopButton.Click, AddressOf StopButton_Click
    End Sub

    ' Méthode à appeler lorsque vous affichez la fenêtre de chargement
    Public Sub SetRelativeDimensions(owner As Window)
        _owner = owner

        ' S'abonner aux événements de redimensionnement de la fenêtre principale
        AddHandler _owner.SizeChanged, AddressOf Owner_SizeChanged

        ' Définir les dimensions initiales
        UpdateDimensions()
    End Sub

    Private Sub Owner_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        ' Mettre à jour les dimensions quand MainWindow change de taille
        UpdateDimensions()
    End Sub

    Private Sub LoadingWindow_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        ' Vérifier que la fenêtre reste dans les limites de l'écran
        EnsureWindowVisibility()
    End Sub

    Private Sub UpdateDimensions()
        If _owner Is Nothing Then Return

        ' Calculer les nouvelles dimensions
        Dim newWidth As Double = Math.Max(_owner.ActualWidth * WIDTH_RATIO, MIN_WIDTH)
        Dim newHeight As Double = Math.Max(_owner.ActualHeight * 0.5, MIN_HEIGHT) ' 50% de la hauteur

        ' Appliquer les nouvelles dimensions
        Me.Width = newWidth
        Me.Height = newHeight

        ' Recentrer la fenêtre par rapport au propriétaire
        If Me.IsLoaded Then
            CenterWindowToOwner()
        End If
    End Sub

    Private Sub CenterWindowToOwner()
        If _owner Is Nothing Then Return

        ' Calculer la position pour centrer la fenêtre par rapport au propriétaire
        Dim left As Double = _owner.Left + (_owner.Width - Me.Width) / 2
        Dim top As Double = _owner.Top + (_owner.Height - Me.Height) / 2

        Me.Left = left
        Me.Top = top
    End Sub

    Private Sub EnsureWindowVisibility()
        ' S'assurer que la fenêtre reste visible sur l'écran
        Dim handle As New WindowInteropHelper(Me).Handle
        Dim screen As Screen = screen.FromHandle(handle)
        Dim workingArea As Rectangle = screen.WorkingArea

        ' Convertir les coordonnées de l'écran en coordonnées WPF
        Dim source As PresentationSource = PresentationSource.FromVisual(Me)
        If source Is Nothing Then Return

        Dim transformToDevice As Matrix = source.CompositionTarget.TransformToDevice
        Dim dpiX As Double = transformToDevice.M11
        Dim dpiY As Double = transformToDevice.M22

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

    ' Méthode à appeler lors de la fermeture de la fenêtre
    Private Sub Cleanup()
        If _owner IsNot Nothing Then
            RemoveHandler _owner.SizeChanged, AddressOf Owner_SizeChanged
            _owner = Nothing
        End If
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        Cleanup()
        MyBase.OnClosed(e)
    End Sub

    Private Sub StopButton_Click(sender As Object, e As RoutedEventArgs)
        ' Demander l'annulation de la collecte
        OSUtility.RequestCancellation()

        ' Déclencher l'événement StopRequested
        RaiseEvent StopRequested(Me, EventArgs.Empty)
    End Sub

    ' Méthode pour mettre à jour le message de chargement
    Public Sub UpdateLoadingMessage(message As String)
        If Not Dispatcher.CheckAccess() Then
            Dispatcher.Invoke(Sub() UpdateLoadingMessage(message))
            Return
        End If

        LoadingMessage.Text = message
    End Sub

    ' Méthode pour mettre à jour le compteur de progression
    Public Sub UpdateProgressCounter(current As Integer, total As Integer)
        If Not Dispatcher.CheckAccess() Then
            Dispatcher.Invoke(Sub() UpdateProgressCounter(current, total))
            Return
        End If

        ProgressCounter.Text = $"{current}/{total}"
    End Sub

    ' Méthode statique à appeler depuis votre code pour afficher la fenêtre
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