' Fichier SettingsEventArgs.vb
Namespace HelloWorldLeChat

    ''' <summary>
    ''' Classe d'arguments d'événement pour transmettre les paramètres appliqués
    ''' </summary>
    Public Class SettingsEventArgs
        Inherits EventArgs

        ''' <summary>
        ''' Obtient ou définit la largeur de la fenêtre
        ''' </summary>
        Public Property WindowWidth As Double

        ''' <summary>
        ''' Obtient ou définit la hauteur de la fenêtre
        ''' </summary>
        Public Property WindowHeight As Double
    End Class


End Namespace