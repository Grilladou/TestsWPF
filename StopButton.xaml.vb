Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Media.Animation

Namespace HelloWorld
    ''' <summary>
    ''' Contrôle utilisateur représentant un bouton STOP unifié
    ''' à utiliser dans toute l'application.
    ''' </summary>
    Public Class StopButton
        Inherits UserControl

        ''' <summary>
        ''' Événement déclenché lorsque l'utilisateur clique sur le bouton STOP.
        ''' </summary>
        Public Event StopClicked As EventHandler

        ''' <summary>
        ''' Animation de clignotement utilisée pendant l'état d'arrêt.
        ''' </summary>
        Private _stoppingStoryboard As Storyboard

        ''' <summary>
        ''' Indique si le bouton est actuellement en état d'arrêt.
        ''' </summary>
        Private _isStopping As Boolean = False

        ''' <summary>
        ''' Constructeur par défaut.
        ''' </summary>
        Public Sub New()
            ' Cette ligne est nécessaire pour l'initialisation des contrôles
            InitializeComponent()

            ' Récupérer l'animation depuis les ressources
            _stoppingStoryboard = CType(Me.FindResource("StoppingAnimation"), Storyboard)
        End Sub

        ''' <summary>
        ''' Gestionnaire de l'événement Click du bouton.
        ''' </summary>
        Private Sub StopButton_Click(sender As Object, e As RoutedEventArgs)
            ' Éviter les clics multiples si déjà en état d'arrêt
            If _isStopping Then Return

            ' Demander l'annulation au niveau global
            BaseInfoCollector.RequestCancellation()

            ' Activer l'état d'arrêt visuel
            SetStoppingState()

            ' Déclencher l'événement pour informer les abonnés
            RaiseEvent StopClicked(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Active l'état visuel d'arrêt (clignotement, changement de texte).
        ''' </summary>
        Public Sub SetStoppingState()
            ' Vérifier si nous sommes sur le thread UI
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(AddressOf SetStoppingState)
                Return
            End If

            ' Éviter d'entrer en état d'arrêt plusieurs fois
            If _isStopping Then Return
            _isStopping = True

            ' Désactiver le bouton pour empêcher d'autres clics
            StopButtonElement.IsEnabled = False

            ' Modifier l'apparence du bouton
            StopButtonElement.Content = "ARRÊT..."

            ' Démarrer l'animation de clignotement
            If _stoppingStoryboard IsNot Nothing Then
                _stoppingStoryboard.Begin()
            End If
        End Sub

        ''' <summary>
        ''' Réinitialise l'état du bouton.
        ''' </summary>
        Public Sub ResetState()
            ' Vérifier si nous sommes sur le thread UI
            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(AddressOf ResetState)
                Return
            End If

            _isStopping = False
            StopButtonElement.IsEnabled = True
            StopButtonElement.Content = "STOP"

            ' Arrêter l'animation si elle est en cours
            If _stoppingStoryboard IsNot Nothing Then
                _stoppingStoryboard.Stop()
            End If
        End Sub
    End Class
End Namespace